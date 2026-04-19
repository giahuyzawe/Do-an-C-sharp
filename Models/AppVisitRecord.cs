using SQLite;

namespace FoodStreetGuide.Models
{
    /// <summary>
    /// Records app visits for analytics tracking
    /// Each time the app is opened, a visit is recorded
    /// </summary>
    public class AppVisitRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Device ID or installation ID
        /// </summary>
        public string? DeviceId { get; set; }
        
        /// <summary>
        /// Timestamp of the visit
        /// </summary>
        public DateTime VisitTime { get; set; }
        
        /// <summary>
        /// Date only for grouping
        /// </summary>
        public DateTime VisitDate { get; set; }
        
        /// <summary>
        /// App version when visit occurred
        /// </summary>
        public string? AppVersion { get; set; }
        
        /// <summary>
        /// Platform (Android/iOS)
        /// </summary>
        public string? Platform { get; set; }
        
        /// <summary>
        /// Session ID for grouping visits
        /// </summary>
        public string? SessionId { get; set; }
    }
}
