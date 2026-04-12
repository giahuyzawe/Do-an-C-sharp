namespace FoodStreetGuide.Admin.Models;

public class MediaFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "image";
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = "";
    public string? PublicUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    public string? AltText { get; set; }
    public string? Tags { get; set; }
    public int? UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public AdminUser? UploadedBy { get; set; }
}
