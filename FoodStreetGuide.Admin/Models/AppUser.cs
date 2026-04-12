namespace FoodStreetGuide.Admin.Models;

public class AppUser
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string DeviceId { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? PreferredLanguage { get; set; } = "vi";
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastActive { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<SavedPlace> SavedPlaces { get; set; } = new List<SavedPlace>();
    public ICollection<UserTracking> Trackings { get; set; } = new List<UserTracking>();
    public ICollection<POIVisit> Visits { get; set; } = new List<POIVisit>();
}

public class SavedPlace
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public int? POIId { get; set; }
    public int? RestaurantId { get; set; }
    public string Type { get; set; } = "poi";
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public AppUser AppUser { get; set; } = null!;
}

public class UserTracking
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AppUser AppUser { get; set; } = null!;
}

public class POIVisit
{
    public int Id { get; set; }
    public int POIId { get; set; }
    public int? AppUserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    public POI POI { get; set; } = null!;
    public AppUser? AppUser { get; set; }
}
