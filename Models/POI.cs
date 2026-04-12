using SQLite;
using System;

namespace FoodStreetGuide.Models
{
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NameVi { get; set; }

        public string NameEn { get; set; }

        public string DescriptionVi { get; set; }

        public string DescriptionEn { get; set; }

        // Category and Tags (synced from Web)
        public string Category { get; set; } = "landmark";

        public string Tags { get; set; }

        public string Status { get; set; } = "active";

        public int VisitCount { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Radius { get; set; }

        public int Priority { get; set; }

        public string Image { get; set; }

        public string AudioVi { get; set; }

        public string AudioEn { get; set; }

        public string MapUrl { get; set; }

        public string Address { get; set; }

        public string OpeningHours { get; set; }

        public string DistanceText { get; set; }

        // Sync tracking
        public DateTime? LastSyncFromWeb { get; set; }
        public DateTime? LastSyncToWeb { get; set; }

        // Computed property to generate Google Maps URL if not provided
        public string GetGoogleMapsUrl()
        {
            if (!string.IsNullOrEmpty(MapUrl))
                return MapUrl;
            
            return $"https://www.google.com/maps?q={Latitude},{Longitude}";
        }
    }
}