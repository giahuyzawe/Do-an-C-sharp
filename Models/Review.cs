using SQLite;
using System;

namespace FoodStreetGuide.Models
{
    public class Review
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Link to POI
        public int POIId { get; set; }

        // Reviewer info
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }

        // Review content
        public int Rating { get; set; } // 1-5 stars
        public string Comment { get; set; }
        public string Images { get; set; } // Comma-separated image URLs

        // Status and moderation
        public string Status { get; set; } = "approved"; // approved, pending, rejected
        public bool IsSpam { get; set; }
        public int SpamReports { get; set; }
        public int HelpfulCount { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Sync tracking
        public DateTime? LastSyncFromWeb { get; set; }
        public string WebReviewId { get; set; } // ID from web admin

        // Computed properties
        [Ignore]
        public string[] ImageList => string.IsNullOrEmpty(Images) 
            ? Array.Empty<string>() 
            : Images.Split(',', StringSplitOptions.RemoveEmptyEntries);

        [Ignore]
        public string TimeAgo => GetTimeAgo(CreatedAt);

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";

            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }
    }
}
