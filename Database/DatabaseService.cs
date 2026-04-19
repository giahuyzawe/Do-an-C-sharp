using SQLite;
using FoodStreetGuide.Models;
using FoodStreetGuide.Database;

namespace FoodStreetGuide.Services
{
    public class DatabaseService : IDatabaseService
    {
        SQLiteAsyncConnection? database;

        public async Task Init()
        {
            System.Diagnostics.Debug.WriteLine("[DatabaseService] Init() called");
            
            if (database != null)
            {
                System.Diagnostics.Debug.WriteLine("[DatabaseService] Database already initialized");
                return;
            }

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "foodstreet.db");
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] DB Path: {dbPath}");

            database = new SQLiteAsyncConnection(dbPath);

            await database.CreateTableAsync<POI>();
            await database.CreateTableAsync<Review>();
            await database.CreateTableAsync<QRScanRecord>();
            await database.CreateTableAsync<AppVisitRecord>();
            await database.CreateTableAsync<AppSessionRecord>();
            await database.CreateTableAsync<POIViewRecord>();
            await database.CreateTableAsync<POICheckInRecord>();
            await database.CreateTableAsync<QRCode>();

            // POIs will be loaded from Web Admin API, not seeded locally
            // Comment out seed data to prevent old POIs from appearing
            /*
            var pois = await database.Table<POI>().ToListAsync();
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] POI count: {pois.Count}");
            
            if (pois.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[DatabaseService] Database is empty, seeding test POIs");
                var testPOIs = SeedData.GetTestPOIs();
                foreach (var poi in testPOIs)
                {
                    await database.InsertAsync(poi);
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] Inserted POI: {poi.NameVi}");
                }
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Seeded {testPOIs.Count} test POIs");
                
                var verifyPois = await database.Table<POI>().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Verification POI count: {verifyPois.Count}");
            }
            */
            System.Diagnostics.Debug.WriteLine("[DatabaseService] POIs will be loaded from Web Admin API");
        }

        public async Task<List<POI>> GetPOIsAsync()
        {
            await Init();
            return await database.Table<POI>().ToListAsync();
        }

        public async Task<POI?> GetPOIAsync(int id)
        {
            await Init();
            return await database.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> AddPOIAsync(POI poi)
        {
            await Init();
            return await database.InsertAsync(poi);
        }

        public async Task<int> DeletePOIAsync(POI poi)
        {
            await Init();
            return await database.DeleteAsync(poi);
        }

        public async Task<int> UpdatePOIAsync(POI poi)
        {
            await Init();
            return await database.UpdateAsync(poi);
        }

        public async Task<int> SavePOIAsync(POI poi)
        {
            await Init();
            
            // Check if POI exists
            var existing = await database.Table<POI>().Where(p => p.Id == poi.Id).FirstOrDefaultAsync();
            
            if (existing != null)
            {
                // Update
                return await database.UpdateAsync(poi);
            }
            else
            {
                // Insert
                return await database.InsertAsync(poi);
            }
        }

        // ==================== REVIEW OPERATIONS ====================

        public async Task<List<Review>> GetReviewsAsync(int? poiId = null)
        {
            await Init();
            
            if (poiId.HasValue)
            {
                return await database.Table<Review>()
                    .Where(r => r.POIId == poiId.Value && r.Status == "approved")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            
            return await database.Table<Review>()
                .Where(r => r.Status == "approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> AddReviewAsync(Review review)
        {
            await Init();
            review.CreatedAt = DateTime.UtcNow;
            return await database.InsertAsync(review);
        }

        public async Task<int> UpdateReviewAsync(Review review)
        {
            await Init();
            review.UpdatedAt = DateTime.UtcNow;
            return await database.UpdateAsync(review);
        }

        public async Task<int> DeleteReviewAsync(Review review)
        {
            await Init();
            return await database.DeleteAsync(review);
        }

        public async Task<int> DeleteReviewByWebIdAsync(string webReviewId)
        {
            await Init();
            var review = await database.Table<Review>()
                .Where(r => r.WebReviewId == webReviewId)
                .FirstOrDefaultAsync();
            
            if (review != null)
            {
                return await database.DeleteAsync(review);
            }
            return 0;
        }

        public async Task<int> SaveReviewAsync(Review review)
        {
            await Init();
            
            var existing = await database.Table<Review>()
                .Where(r => r.Id == review.Id)
                .FirstOrDefaultAsync();
            
            if (existing != null)
            {
                review.UpdatedAt = DateTime.UtcNow;
                return await database.UpdateAsync(review);
            }
            else
            {
                review.CreatedAt = DateTime.UtcNow;
                return await database.InsertAsync(review);
            }
        }

        public async Task<int> GetReviewCountAsync(int poiId)
        {
            await Init();
            return await database.Table<Review>()
                .Where(r => r.POIId == poiId && r.Status == "approved")
                .CountAsync();
        }

        public async Task<double> GetAverageRatingAsync(int poiId)
        {
            await Init();
            var reviews = await database.Table<Review>()
                .Where(r => r.POIId == poiId && r.Status == "approved")
                .ToListAsync();
            
            if (reviews.Count == 0) return 0;
            
            return reviews.Average(r => r.Rating);
        }

        // ==================== QR SCAN TRACKING ====================

        /// <summary>
        /// Records a QR scan and determines if it should count as a visit
        /// Implements 1-hour cooldown between scans of the same POI
        /// </summary>
        public async Task<(bool IsDuplicate, TimeSpan? TimeRemaining)> RecordQRScanAsync(int poiId, string deviceId, string? qrContent)
        {
            await Init();
            
            // Check for recent scan (within 1 hour cooldown)
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentScan = await database.Table<QRScanRecord>()
                .Where(s => s.POIId == poiId && s.DeviceId == deviceId && s.ScanTime > oneHourAgo)
                .OrderByDescending(s => s.ScanTime)
                .FirstOrDefaultAsync();

            if (recentScan != null)
            {
                // Duplicate scan within cooldown period
                var timeRemaining = TimeSpan.FromHours(1) - (DateTime.UtcNow - recentScan.ScanTime);
                return (true, timeRemaining);
            }

            // New scan - record it
            var record = new QRScanRecord
            {
                POIId = poiId,
                DeviceId = deviceId,
                ScanTime = DateTime.UtcNow,
                CountedAsVisit = true,
                QRContent = qrContent
            };

            await database.InsertAsync(record);

            // Increment visit count for the POI
            await IncrementPOIVisitCountAsync(poiId);

            return (false, null);
        }

        /// <summary>
        /// Gets the last scan time for a specific POI and device
        /// </summary>
        public async Task<DateTime?> GetLastScanTimeAsync(int poiId, string deviceId)
        {
            await Init();
            
            var lastScan = await database.Table<QRScanRecord>()
                .Where(s => s.POIId == poiId && s.DeviceId == deviceId)
                .OrderByDescending(s => s.ScanTime)
                .FirstOrDefaultAsync();

            return lastScan?.ScanTime;
        }

        /// <summary>
        /// Increments the visit count for a POI
        /// </summary>
        private async Task IncrementPOIVisitCountAsync(int poiId)
        {
            var poi = await database.Table<POI>().Where(p => p.Id == poiId).FirstOrDefaultAsync();
            if (poi != null)
            {
                poi.VisitCount++;
                await database.UpdateAsync(poi);
            }
        }

        // ==================== ANALYTICS ====================

        /// <summary>
        /// Records an app visit (each time app is opened)
        /// </summary>
        public async Task RecordAppVisitAsync(string deviceId)
        {
            await Init();
            
            var visit = new AppVisitRecord
            {
                DeviceId = deviceId,
                VisitTime = DateTime.UtcNow,
                VisitDate = DateTime.UtcNow.Date
            };

            await database.InsertAsync(visit);
        }

        /// <summary>
        /// Gets total app visits count
        /// </summary>
        public async Task<int> GetTotalAppVisitsAsync()
        {
            await Init();
            return await database.Table<AppVisitRecord>().CountAsync();
        }

        /// <summary>
        /// Gets unique device count
        /// </summary>
        public async Task<int> GetUniqueDeviceCountAsync()
        {
            await Init();
            var devices = await database.Table<AppVisitRecord>()
                .ToListAsync();
            return devices.Select(v => v.DeviceId).Distinct().Count();
        }

        /// <summary>
        /// Gets today's visit count
        /// </summary>
        public async Task<int> GetTodayVisitsAsync()
        {
            await Init();
            var today = DateTime.UtcNow.Date;
            return await database.Table<AppVisitRecord>()
                .Where(v => v.VisitDate == today)
                .CountAsync();
        }

        // ==================== DYNAMIC QR CODE ====================

        /// <summary>
        /// Validates a dynamic QR code and returns the associated POI if valid
        /// </summary>
        public async Task<(bool IsValid, POI? POI, string? ErrorMessage)> ValidateDynamicQRAsync(string uniqueToken)
        {
            await Init();
            
            // Find QR code by unique token
            var qrCode = await database.Table<QRCode>()
                .Where(q => q.UniqueToken == uniqueToken)
                .FirstOrDefaultAsync();

            if (qrCode == null)
            {
                return (false, null, "Mã QR không tồn tại trong hệ thống");
            }

            // Check if valid
            if (!qrCode.IsValid())
            {
                if (qrCode.ExpiresAt.HasValue && DateTime.UtcNow > qrCode.ExpiresAt.Value)
                {
                    return (false, null, "Mã QR đã hết hạn sử dụng");
                }
                
                if (qrCode.MaxScans.HasValue && qrCode.ScanCount >= qrCode.MaxScans.Value)
                {
                    return (false, null, "Mã QR đã đạt giới hạn số lần quét");
                }
                
                if (qrCode.QRType == "single_use" && qrCode.IsUsed)
                {
                    return (false, null, "Mã QR đã được sử dụng (chỉ dùng 1 lần)");
                }
                
                return (false, null, "Mã QR không còn hiệu lực");
            }

            // Get associated POI
            var poi = await database.Table<POI>()
                .Where(p => p.Id == qrCode.POIId)
                .FirstOrDefaultAsync();

            if (poi == null)
            {
                return (false, null, "Không tìm thấy nhà hàng liên kết với mã QR này");
            }

            // Check POI approval status
            if (poi.ApprovalStatus != "approved")
            {
                return (false, null, "Nhà hàng này chưa được duyệt");
            }

            // Update scan count
            qrCode.ScanCount++;
            if (qrCode.QRType == "single_use")
            {
                qrCode.IsUsed = true;
            }
            await database.UpdateAsync(qrCode);

            return (true, poi, null);
        }

        /// <summary>
        /// Gets all QR codes for a specific POI
        /// </summary>
        public async Task<List<QRCode>> GetQRCodesForPOIAsync(int poiId)
        {
            await Init();
            return await database.Table<QRCode>()
                .Where(q => q.POIId == poiId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets QR code statistics
        /// </summary>
        public async Task<(int Total, int Valid, int Expired, int SingleUseUsed)> GetQRCodeStatsAsync()
        {
            await Init();
            
            var allQRCodes = await database.Table<QRCode>().ToListAsync();
            
            int total = allQRCodes.Count;
            int valid = allQRCodes.Count(q => q.IsValid());
            int expired = allQRCodes.Count(q => q.ExpiresAt.HasValue && DateTime.UtcNow > q.ExpiresAt.Value);
            int singleUseUsed = allQRCodes.Count(q => q.QRType == "single_use" && q.IsUsed);
            
            return (total, valid, expired, singleUseUsed);
        }

        // ==================== ANALYTICS METHODS ====================

        /// <summary>
        /// Records an app visit for DAU calculation
        /// </summary>
        public async Task RecordAppVisitAsync(string deviceId, DateTime visitTime, string? appVersion = null, string? platform = null)
        {
            await Init();
            
            var visit = new AppVisitRecord
            {
                DeviceId = deviceId,
                VisitDate = visitTime.Date,
                VisitTime = visitTime,
                SessionId = Guid.NewGuid().ToString("N")[..16],
                AppVersion = appVersion ?? "1.0",
                Platform = platform ?? DeviceInfo.Platform.ToString()
            };
            
            await database.InsertAsync(visit);
            System.Diagnostics.Debug.WriteLine($"[Analytics] App visit recorded: {deviceId} at {visitTime}");
        }

        /// <summary>
        /// Starts a new session for App Opens tracking
        /// </summary>
        public async Task<string> StartNewSessionAsync(string deviceId)
        {
            await Init();
            
            var sessionId = Guid.NewGuid().ToString("N")[..16];
            var session = new AppSessionRecord
            {
                DeviceId = deviceId,
                SessionId = sessionId,
                SessionStart = DateTime.Now,
                OpenCount = 1
            };
            
            await database.InsertAsync(session);
            System.Diagnostics.Debug.WriteLine($"[Analytics] New session started: {sessionId} for {deviceId}");
            
            return sessionId;
        }

        /// <summary>
        /// Records an app open within a session
        /// </summary>
        public async Task RecordAppOpenAsync(string deviceId, string sessionId)
        {
            await Init();
            
            var session = await database.Table<AppSessionRecord>()
                .Where(s => s.SessionId == sessionId)
                .FirstOrDefaultAsync();
            
            if (session != null)
            {
                session.OpenCount++;
                session.SessionEnd = DateTime.Now;
                await database.UpdateAsync(session);
                System.Diagnostics.Debug.WriteLine($"[Analytics] App open recorded, count: {session.OpenCount}");
            }
        }

        /// <summary>
        /// Gets the last activity time for a device
        /// </summary>
        public async Task<DateTime?> GetLastActivityAsync(string deviceId)
        {
            await Init();
            
            var lastSession = await database.Table<AppSessionRecord>()
                .Where(s => s.DeviceId == deviceId)
                .OrderByDescending(s => s.SessionEnd)
                .FirstOrDefaultAsync();
            
            return lastSession?.SessionEnd;
        }

        /// <summary>
        /// Records a POI view with cooldown check (10 minutes)
        /// </summary>
        public async Task<bool> RecordPOIViewAsync(int poiId, string deviceId, string source, int? durationSeconds = null)
        {
            await Init();
            
            // Check cooldown (10 minutes)
            var cooldown = TimeSpan.FromMinutes(10);
            var lastView = await database.Table<POIViewRecord>()
                .Where(v => v.POIId == poiId && v.DeviceId == deviceId)
                .OrderByDescending(v => v.ViewTime)
                .FirstOrDefaultAsync();
            
            if (lastView != null && (DateTime.Now - lastView.ViewTime) < cooldown)
            {
                System.Diagnostics.Debug.WriteLine($"[Analytics] POI view cooldown, skipping: {poiId}");
                return false;
            }
            
            var view = new POIViewRecord
            {
                POIId = poiId,
                DeviceId = deviceId,
                ViewTime = DateTime.Now,
                Source = source,
                SessionId = await GetCurrentSessionIdAsync(deviceId),
                DurationSeconds = durationSeconds
            };
            
            await database.InsertAsync(view);
            
            // Increment POI visit count
            var poi = await database.Table<POI>().Where(p => p.Id == poiId).FirstOrDefaultAsync();
            if (poi != null)
            {
                poi.VisitCount++;
                await database.UpdateAsync(poi);
            }
            
            System.Diagnostics.Debug.WriteLine($"[Analytics] POI view recorded: {poiId} from {source}");
            return true;
        }

        /// <summary>
        /// Records a QR check-in
        /// </summary>
        public async Task RecordCheckInAsync(int poiId, string deviceId, string? qrToken = null, double? lat = null, double? lng = null)
        {
            await Init();
            
            var checkIn = new POICheckInRecord
            {
                POIId = poiId,
                DeviceId = deviceId,
                CheckInTime = DateTime.Now,
                QRToken = qrToken,
                Latitude = lat,
                Longitude = lng
            };
            
            await database.InsertAsync(checkIn);
            System.Diagnostics.Debug.WriteLine($"[Analytics] Check-in recorded: {poiId}, token: {qrToken}");
        }

        /// <summary>
        /// Gets current session ID for a device
        /// </summary>
        private async Task<string?> GetCurrentSessionIdAsync(string deviceId)
        {
            var lastSession = await database.Table<AppSessionRecord>()
                .Where(s => s.DeviceId == deviceId)
                .OrderByDescending(s => s.SessionStart)
                .FirstOrDefaultAsync();
            
            return lastSession?.SessionId;
        }

        /// <summary>
        /// Gets analytics summary for a date range
        /// </summary>
        public async Task<AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime startDate, DateTime endDate)
        {
            await Init();
            
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            
            // DAU - Unique devices per day
            var dauData = await database.Table<AppVisitRecord>()
                .Where(v => v.VisitDate >= start && v.VisitDate < end)
                .ToListAsync();
            
            var dau = dauData.GroupBy(v => new { v.VisitDate, v.DeviceId }).Count();
            
            // App Opens - Total sessions
            var sessions = await database.Table<AppSessionRecord>()
                .Where(s => s.SessionStart >= start && s.SessionStart < end)
                .ToListAsync();
            
            var appOpens = sessions.Sum(s => s.OpenCount);
            
            // POI Views
            var poiViews = await database.Table<POIViewRecord>()
                .Where(v => v.ViewTime >= start && v.ViewTime < end)
                .CountAsync();
            
            // QR Check-ins
            var checkIns = await database.Table<POICheckInRecord>()
                .Where(c => c.CheckInTime >= start && c.CheckInTime < end)
                .CountAsync();
            
            // Top 5 POIs by views (fetch then group in memory for SQLite compatibility)
            var viewRecords = await database.Table<POIViewRecord>()
                .Where(v => v.ViewTime >= start && v.ViewTime < end)
                .ToListAsync();
            
            var topPOIs = viewRecords
                .GroupBy(v => v.POIId)
                .Select(g => new { POIId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            
            var topPOIList = new List<TopPOI>();
            foreach (var item in topPOIs)
            {
                var poi = await database.Table<POI>().Where(p => p.Id == item.POIId).FirstOrDefaultAsync();
                if (poi != null)
                {
                    topPOIList.Add(new TopPOI { POIId = item.POIId, Name = poi.NameVi, ViewCount = item.Count });
                }
            }
            
            return new AnalyticsSummary
            {
                DAU = dau,
                AppOpens = appOpens,
                POIViews = poiViews,
                CheckIns = checkIns,
                TopPOIs = topPOIList
            };
        }

        /// <summary>
        /// Gets analytics for a specific POI
        /// </summary>
        public async Task<POIAnalytics> GetPOIAnalyticsAsync(int poiId, DateTime startDate, DateTime endDate)
        {
            await Init();
            
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            
            // Total views
            var totalViews = await database.Table<POIViewRecord>()
                .Where(v => v.POIId == poiId && v.ViewTime >= start && v.ViewTime < end)
                .CountAsync();
            
            // Unique viewers (fetch then group in memory)
            var viewerRecords = await database.Table<POIViewRecord>()
                .Where(v => v.POIId == poiId && v.ViewTime >= start && v.ViewTime < end)
                .ToListAsync();
            var uniqueViewers = viewerRecords.Select(v => v.DeviceId).Distinct().Count();
            
            // Check-ins
            var checkIns = await database.Table<POICheckInRecord>()
                .Where(c => c.POIId == poiId && c.CheckInTime >= start && c.CheckInTime < end)
                .CountAsync();
            
            // Conversion rate
            var conversionRate = totalViews > 0 ? (double)checkIns / totalViews * 100 : 0;
            
            return new POIAnalytics
            {
                POIId = poiId,
                TotalViews = totalViews,
                UniqueViewers = uniqueViewers,
                CheckIns = checkIns,
                ConversionRate = Math.Round(conversionRate, 1)
            };
        }
    }

    // Analytics Models
    public class AppSessionRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
        public DateTime? SessionEnd { get; set; }
        public int OpenCount { get; set; } = 1;
    }

    public class POIViewRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int POIId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime ViewTime { get; set; }
        public string Source { get; set; } = string.Empty; // "map", "list", "qr", "search"
        public string? SessionId { get; set; }
        public int? DurationSeconds { get; set; }
    }

    public class POICheckInRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int POIId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public string? QRToken { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class AnalyticsSummary
    {
        public int DAU { get; set; }
        public int AppOpens { get; set; }
        public int POIViews { get; set; }
        public int CheckIns { get; set; }
        public List<TopPOI> TopPOIs { get; set; } = new();
    }

    public class TopPOI
    {
        public int POIId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ViewCount { get; set; }
    }

    public class POIAnalytics
    {
        public int POIId { get; set; }
        public int TotalViews { get; set; }
        public int UniqueViewers { get; set; }
        public int CheckIns { get; set; }
        public double ConversionRate { get; set; }
    }
}