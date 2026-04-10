namespace FoodStreetGuide.Admin.Models;

public class POI
{
    public int Id { get; set; }
    public string NameVi { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string DescriptionVi { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public string? AudioVi { get; set; }
    public string? AudioEn { get; set; }
    public string? MapUrl { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
