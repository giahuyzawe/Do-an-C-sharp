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
    }
}