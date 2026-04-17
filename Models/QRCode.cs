using SQLite;
using System;

namespace FoodStreetGuide.Models
{
    /// <summary>
    /// Dynamic QR Code - Each generation creates a unique code
    /// Can have expiration time and usage limits
    /// </summary>
    public class QRCode
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Unique token for this QR code (e.g., "vk-2024-abc123xyz")
        /// </summary>
        public string? UniqueToken { get; set; }
        
        /// <summary>
        /// Associated POI ID
        /// </summary>
        public int POIId { get; set; }
        
        /// <summary>
        /// QR Code type: 'single_use', 'time_limited', 'unlimited'
        /// </summary>
        public string? QRType { get; set; } = "time_limited";
        
        /// <summary>
        /// When the QR code was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the QR code expires (null = never)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Maximum number of scans allowed (null = unlimited)
        /// </summary>
        public int? MaxScans { get; set; }
        
        /// <summary>
        /// Current scan count
        /// </summary>
        public int ScanCount { get; set; }
        
        /// <summary>
        /// Whether this QR has been marked as used (for single_use type)
        /// </summary>
        public bool IsUsed { get; set; }
        
        /// <summary>
        /// Who created this QR code
        /// </summary>
        public string? CreatedBy { get; set; }
        
        /// <summary>
        /// Notes or description
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Check if QR code is still valid
        /// </summary>
        public bool IsValid()
        {
            // Check if expired
            if (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value)
            {
                return false;
            }
            
            // Check if max scans reached
            if (MaxScans.HasValue && ScanCount >= MaxScans.Value)
            {
                return false;
            }
            
            // Check if already used (single use)
            if (QRType == "single_use" && IsUsed)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get remaining time before expiry
        /// </summary>
        public TimeSpan? GetRemainingTime()
        {
            if (!ExpiresAt.HasValue) return null;
            return ExpiresAt.Value - DateTime.UtcNow;
        }
        
        /// <summary>
        /// Get remaining scans allowed
        /// </summary>
        public int? GetRemainingScans()
        {
            if (!MaxScans.HasValue) return null;
            return MaxScans.Value - ScanCount;
        }
        
        /// <summary>
        /// Generate deep link URL for this QR code
        /// </summary>
        public string GetDeepLink()
        {
            return $"foodstreetguide://qr/{UniqueToken}";
        }
    }
}
