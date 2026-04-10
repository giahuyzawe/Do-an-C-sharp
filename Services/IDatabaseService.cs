using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface IDatabaseService
{
    Task Init();
    Task<List<POI>> GetPOIsAsync();
    Task<int> AddPOIAsync(POI poi);
    Task<int> UpdatePOIAsync(POI poi);
    Task<int> DeletePOIAsync(POI poi);
    Task<int> SavePOIAsync(POI poi);
}
