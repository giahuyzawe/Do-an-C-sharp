using Microsoft.AspNetCore.Identity;

namespace FoodStreetGuide.Admin.Models;

public class AdminUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Editor";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}

public class GeofenceConfig
{
    public int Id { get; set; }
    public int? POIId { get; set; }
    public int? RestaurantId { get; set; }
    public string TriggerType { get; set; } = "enter";
    public int Radius { get; set; } = 100;
    public int CooldownMinutes { get; set; } = 30;
    public int Priority { get; set; } = 1;
    public POI? POI { get; set; }
    public Restaurant? Restaurant { get; set; }
}

public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "general";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class OfflinePackage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? RegionName { get; set; }
    public double? CenterLat { get; set; }
    public double? CenterLng { get; set; }
    public double? RadiusKm { get; set; }
    public string? IncludedPOIs { get; set; }
    public string? IncludedRestaurants { get; set; }
    public long? PackageSizeBytes { get; set; }
    public string? DownloadUrl { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
