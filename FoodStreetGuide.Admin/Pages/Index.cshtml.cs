using FoodStreetGuide.Admin.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetGuide.Admin.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public int TotalPOIs { get; set; }
    public int HighPriorityPOIs { get; set; }
    public int POIsWithAudio { get; set; }
    public int RecentPOIs { get; set; }
    public List<Models.POI> RecentPOIList { get; set; } = new();

    public void OnGet()
    {
        var pois = _context.POIs;
        
        TotalPOIs = pois.Count;
        HighPriorityPOIs = pois.Count(p => p.Priority >= 3);
        POIsWithAudio = pois.Count(p => !string.IsNullOrEmpty(p.AudioVi) || !string.IsNullOrEmpty(p.AudioEn));
        RecentPOIs = pois.Count(p => p.CreatedAt >= DateTime.Now.AddDays(-7));
        
        RecentPOIList = pois.OrderByDescending(p => p.CreatedAt).Take(5).ToList();
    }
}
