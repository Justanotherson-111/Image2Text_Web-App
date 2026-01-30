using backend.Models;
using backend.Database;
using backend.Services.Interfaces;
using backend.Services.ServiceDef;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using backend.Hubs;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json.Serialization;
using System.Text.Json;

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
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// ========================= SWAGGER ===========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================= SERVICES ==========================
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRateLimiterService, RateLimiterService>();
builder.Services.AddScoped<TesseractOcrService>();
builder.Services.AddHttpClient<PaddleOcrService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<ICorrector, Corrector>();
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
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
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
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // 1️⃣ Authorization header (default)
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authHeader["Bearer ".Length..];
                return Task.CompletedTask;
            }

            // 2️⃣ Optional: access token from cookie
            if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken) &&
                !string.IsNullOrWhiteSpace(cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
});

// =========================== REAL TIME =======================
builder.Services.AddSignalR(); // currently unused for future updated
// =========================== PDF LIB =========================
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
// ========================== SERVE STATIC FILE ============================
// Serve uploaded images
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// Serve extracted text files
var textPath = Path.Combine(Directory.GetCurrentDirectory(), "ExtractedText");
Directory.CreateDirectory(textPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(textPath),
    RequestPath = "/extracted-text"
});


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
