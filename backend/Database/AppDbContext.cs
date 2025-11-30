using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<TextFile> TextFiles { get; set; }
        public DbSet<OcrJob> OcrJobs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
            modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

            modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
            .HasMany(u => u.Images)
            .WithOne(i => i.UploadedBy)
            .HasForeignKey(i => i.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Image>()
            .HasOne(i => i.OcrJob)
            .WithOne(o => o.Image)
            .HasForeignKey<OcrJob>(o => o.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Image>()
            .HasMany(i => i.TextFiles)
            .WithOne(t => t.Image)
            .HasForeignKey(t => t.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TextFile>()
            .HasOne(t => t.CreatedBy)
            .WithMany(u => u.TextFiles)
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}