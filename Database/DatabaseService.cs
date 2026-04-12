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

            // Seed data if empty
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
                
                // Verify insertion
                var verifyPois = await database.Table<POI>().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Verification POI count: {verifyPois.Count}");
            }
        }

        public async Task<List<POI>> GetPOIsAsync()
        {
            await Init();
            return await database.Table<POI>().ToListAsync();
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
    }
}