using backend.Models;
using backend.Database;
using backend.Services.Interfaces;
using backend.Services.ServiceDef;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.DataProtection;
using backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ======================= DATA PROTECTION =======================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("MyProject");

// ========================= DATABASE ==========================
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ========================= CONTROLLERS =======================
builder.Services.AddControllers();

// ========================= SWAGGER ===========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================= SERVICES ==========================
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRateLimiterService, RateLimiterService>();
builder.Services.AddScoped<ITesseractOcrService, TesseractOcrService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<OCRBackgroundService>();

// ========================= CORS =============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://localhost"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ========================= AUTHENTICATION =====================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role
    };
});
// =========================== REAL TIME =======================
builder.Services.AddSignalR();

// ========================= BUILD APP ==========================
var app = builder.Build();

// ========================= MIGRATION & SEED ===================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        await SeedAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Migration/seeding failed.");
    }
}

// ========================= MIDDLEWARE ==========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Needed when behind Docker / reverse proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowLocalhost");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OcrHub>("/api/hubs/ocr");
app.Run();

// ========================= ADMIN SEED ==========================
static async Task SeedAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!await db.Users.AnyAsync(u => u.UserRole == Role.Admin))
    {
        var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
        var hashed = BCrypt.Net.BCrypt.HashPassword(adminPass);

        var admin = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = hashed,
            UserRole = Role.Admin
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Default admin created: admin@example.com / {Password}", adminPass);
    }
}
