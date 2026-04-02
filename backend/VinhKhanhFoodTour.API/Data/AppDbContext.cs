using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet properties for all entities
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Poi> Pois { get; set; } = null!;
        public DbSet<PoiTranslation> PoiTranslations { get; set; } = null!;
        public DbSet<NarrationLog> NarrationLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============== Role Configuration ==============
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.RoleName).IsRequired().HasMaxLength(50);

                // One-to-Many: Role -> Users
                entity.HasMany(r => r.Users)
                    .WithOne(u => u.Role)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============== User Configuration ==============
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.RoleId).IsRequired();

                // One-to-Many: User -> Pois (Owner)
                entity.HasMany(u => u.OwnedPois)
                    .WithOne(p => p.Owner)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============== Poi Configuration ==============
            modelBuilder.Entity<Poi>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
                
                // Configure spatial property with SRID 4326 (WGS84 - latitude/longitude)
                entity.Property(p => p.Location)
                    .IsRequired()
                    .HasColumnType("geography");
                
                entity.Property(p => p.TriggerRadius).HasDefaultValue(20.0);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Configure PoiStatus enum as string instead of integer
                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasDefaultValue(PoiStatus.Pending);

                // One-to-Many: Poi -> PoiTranslations
                entity.HasMany(p => p.Translations)
                    .WithOne(pt => pt.Poi)
                    .HasForeignKey(pt => pt.PoiId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One-to-Many: Poi -> NarrationLogs
                entity.HasMany(p => p.NarrationLogs)
                    .WithOne(nl => nl.Poi)
                    .HasForeignKey(nl => nl.PoiId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============== PoiTranslation Configuration ==============
            modelBuilder.Entity<PoiTranslation>(entity =>
            {
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.PoiId).IsRequired();
                entity.Property(pt => pt.LanguageCode).IsRequired().HasMaxLength(10);
                entity.Property(pt => pt.Title).IsRequired().HasMaxLength(255);
                entity.Property(pt => pt.Description).IsRequired(false);
                entity.Property(pt => pt.AudioFilePath).IsRequired(false);
                entity.Property(pt => pt.ImageUrl).IsRequired(false);
            });

            // ============== NarrationLog Configuration ==============
            modelBuilder.Entity<NarrationLog>(entity =>
            {
                entity.HasKey(nl => nl.Id);
                entity.Property(nl => nl.PoiId).IsRequired();
                entity.Property(nl => nl.DeviceId).IsRequired().HasMaxLength(255);
                entity.Property(nl => nl.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
