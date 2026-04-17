using SQLite;

namespace FoodStreetGuide.Models
{
    /// <summary>
    /// Record of QR code scans to prevent duplicate counting
    /// Implements cooldown logic for visit tracking
    /// </summary>
    public class QRScanRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// POI ID that was scanned
        /// </summary>
        public int POIId { get; set; }
        
        /// <summary>
        /// Device ID or installation ID (unique per device)
        /// </summary>
        public string? DeviceId { get; set; }
        
        /// <summary>
        /// Timestamp of the scan
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Whether this scan counted as a visit
        /// </summary>
        public bool CountedAsVisit { get; set; }
        
        /// <summary>
        /// QR code content that was scanned
        /// </summary>
        public string? QRContent { get; set; }
    }
}
