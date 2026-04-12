namespace FoodStreetGuide.Admin.Models;

public class SystemLog
{
    public int Id { get; set; }
    public string Level { get; set; } = "Info";
    public string Category { get; set; } = "general";
    public string Message { get; set; } = "";
    public string? Details { get; set; }
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class LoginLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string TargetType { get; set; } = "all";
    public string? TargetUsers { get; set; }
    public double? TargetLat { get; set; }
    public double? TargetLng { get; set; }
    public double? TargetRadiusKm { get; set; }
    public int? SentCount { get; set; }
    public int? DeliveredCount { get; set; }
    public int? OpenedCount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedById { get; set; }
    public AdminUser? CreatedBy { get; set; }
}

public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string[]? Permissions { get; set; }
    public string? AllowedIps { get; set; }
    public int? RateLimitPerMinute { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedById { get; set; }
    public AdminUser? CreatedBy { get; set; }
}
