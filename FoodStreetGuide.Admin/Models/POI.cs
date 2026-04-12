namespace FoodStreetGuide.Admin.Models;

public class POI
{
    public int Id { get; set; }
    public string NameVi { get; set; } = "";
    public string? NameEn { get; set; }
    public string? DescriptionVi { get; set; }
    public string? DescriptionEn { get; set; }
    public string Category { get; set; } = "landmark";
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public string? NarrationType { get; set; } = "tts";
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "active";
    public int VisitCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Narration> Narrations { get; set; } = new List<Narration>();
    public ICollection<POIVisit> Visits { get; set; } = new List<POIVisit>();
}

// NetTopologySuite Point for spatial data
public class Point
{
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
    public double X { get; set; }
    public double Y { get; set; }
    public int SRID { get; set; }
}
