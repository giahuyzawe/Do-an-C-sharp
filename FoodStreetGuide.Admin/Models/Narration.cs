namespace FoodStreetGuide.Admin.Models;

public class Narration
{
    public int Id { get; set; }
    public int POIId { get; set; }
    public string Language { get; set; } = "vi";
    public string Type { get; set; } = "tts";
    public string? TextScript { get; set; }
    public string? VoiceId { get; set; }
    public string? AudioUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public POI POI { get; set; } = null!;
    public ICollection<NarrationPlay> Plays { get; set; } = new List<NarrationPlay>();
}

public class NarrationPlay
{
    public int Id { get; set; }
    public int NarrationId { get; set; }
    public int? AppUserId { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public bool Completed { get; set; }
    public Narration Narration { get; set; } = null!;
    public AppUser? AppUser { get; set; }
}
