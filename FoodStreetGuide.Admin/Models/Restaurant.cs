namespace FoodStreetGuide.Admin.Models;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Point Location => new Point(Longitude, Latitude) { SRID = 4326 };
    public string Category { get; set; } = "street_food";
    public string? OpenHours { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? PriceRange { get; set; } = "$";
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public string? Images { get; set; }
    public bool IsHighlighted { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<RestaurantReview> Reviews { get; set; } = new List<RestaurantReview>();
}

public class MenuItem
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPopular { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
}

public class RestaurantReview
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int? AppUserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Restaurant Restaurant { get; set; } = null!;
    public AppUser? AppUser { get; set; }
}
