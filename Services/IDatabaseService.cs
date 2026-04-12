using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface IDatabaseService
{
    Task Init();
    
    // POI operations
    Task<List<POI>> GetPOIsAsync();
    Task<int> AddPOIAsync(POI poi);
    Task<int> UpdatePOIAsync(POI poi);
    Task<int> DeletePOIAsync(POI poi);
    Task<int> SavePOIAsync(POI poi);
    
    // Review operations
    Task<List<Review>> GetReviewsAsync(int? poiId = null);
    Task<int> AddReviewAsync(Review review);
    Task<int> UpdateReviewAsync(Review review);
    Task<int> DeleteReviewAsync(Review review);
    Task<int> SaveReviewAsync(Review review);
    Task<int> GetReviewCountAsync(int poiId);
    Task<double> GetAverageRatingAsync(int poiId);
}
