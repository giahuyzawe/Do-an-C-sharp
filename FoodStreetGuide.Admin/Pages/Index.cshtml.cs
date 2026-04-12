using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    // Stats Cards
    public int TotalPOIs { get; set; }
    public int TotalRestaurants { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveTrackingToday { get; set; }
    public int TotalNarrationsPlayed { get; set; }
    public double AverageRating { get; set; }
    public int HighPriorityPOIs { get; set; }
    public int POIsWithAudio { get; set; }
    public int RecentPOIs { get; set; }

    // Chart Data
    public List<int> VisitsPerDay { get; set; } = new();
    public List<string> DayLabels { get; set; } = new();
    public List<POIVisitStats> TopPOIs { get; set; } = new();
    public List<RestaurantRatingStats> TopRestaurants { get; set; } = new();

    // Recent Data
    public List<POI> RecentPOIList { get; set; } = new();
    public List<AppUser> RecentUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;
        var sevenDaysAgo = today.AddDays(-7);

        // Stats
        TotalPOIs = await _db.POIs.CountAsync();
        TotalRestaurants = await _db.Restaurants.CountAsync();
        TotalUsers = await _db.AppUsers.CountAsync();
        ActiveTrackingToday = await _db.UserTrackings
            .Where(t => t.Timestamp >= today)
            .Select(t => t.AppUserId)
            .Distinct()
            .CountAsync();
        TotalNarrationsPlayed = await _db.NarrationPlays.CountAsync();
        AverageRating = await _db.Restaurants.AverageAsync(r => (double?)r.Rating) ?? 0;
        HighPriorityPOIs = await _db.POIs.CountAsync(p => p.Priority >= 3);
        POIsWithAudio = await _db.POIs.CountAsync(p => !string.IsNullOrEmpty(p.AudioUrl));
        RecentPOIs = await _db.POIs.CountAsync(p => p.CreatedAt >= sevenDaysAgo);

        // Recent lists
        RecentPOIList = await _db.POIs
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        RecentUsers = await _db.AppUsers
            .OrderByDescending(u => u.JoinDate)
            .Take(5)
            .ToListAsync();

        // Chart: Visits per day (last 7 days)
        DayLabels = Enumerable.Range(0, 7)
            .Select(i => sevenDaysAgo.AddDays(i).ToString("ddd"))
            .ToList();

        for (int i = 0; i < 7; i++)
        {
            var date = sevenDaysAgo.AddDays(i);
            var count = await _db.POIVisits
                .CountAsync(v => v.VisitedAt.Date == date);
            VisitsPerDay.Add(count);
        }

        // Top POIs
        TopPOIs = await _db.POIs
            .Where(p => p.Status == "active")
            .OrderByDescending(p => p.VisitCount)
            .Take(5)
            .Select(p => new POIVisitStats
            {
                Name = p.NameVi,
                Visits = p.VisitCount
            })
            .ToListAsync();

        // Top Rated Restaurants
        TopRestaurants = await _db.Restaurants
            .Where(r => r.Status == "active" && r.RatingCount > 0)
            .OrderByDescending(r => r.Rating)
            .Take(5)
            .Select(r => new RestaurantRatingStats
            {
                Name = r.Name,
                Rating = r.Rating,
                Count = r.RatingCount
            })
            .ToListAsync();
    }
}

public class POIVisitStats
{
    public string Name { get; set; } = "";
    public int Visits { get; set; }
}

public class RestaurantRatingStats
{
    public string Name { get; set; } = "";
    public double Rating { get; set; }
    public int Count { get; set; }
}
