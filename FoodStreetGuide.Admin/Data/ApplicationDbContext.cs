using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Data;

public class ApplicationDbContext : IdentityDbContext<AdminUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // POI & Location
    public DbSet<POI> POIs => Set<POI>();
    public DbSet<POIVisit> POIVisits => Set<POIVisit>();
    public DbSet<GeofenceConfig> GeofenceConfigs => Set<GeofenceConfig>();

    // Restaurants
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RestaurantReview> RestaurantReviews => Set<RestaurantReview>();

    // Content
    public DbSet<Narration> Narrations => Set<Narration>();
    public DbSet<NarrationPlay> NarrationPlays => Set<NarrationPlay>();

    // Users & Mobile App
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();
    public DbSet<UserTracking> UserTrackings => Set<UserTracking>();

    // Media
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    // Settings & Config
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<OfflinePackage> OfflinePackages => Set<OfflinePackage>();

    // System
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // POI
        builder.Entity<POI>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.NameVi).HasMaxLength(200);
            entity.Property(e => e.NameEn).HasMaxLength(200);
            entity.Property(e => e.Status).HasDefaultValue("active");
        });

        // Restaurant
        builder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasDefaultValue("active");
        });

        // AppUser
        builder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.Status).HasDefaultValue("active");
        });

        // Narration
        builder.Entity<Narration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.POIId);
            entity.HasIndex(e => e.Language);
            entity.Property(e => e.Language).HasMaxLength(10);
        });

        // MediaFile
        builder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileType);
            entity.Property(e => e.FileType).HasMaxLength(20);
        });

        // API Keys
        builder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).HasMaxLength(100);
        });
    }
}
