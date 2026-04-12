using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages.Restaurants;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<Restaurant> Restaurants { get; set; } = new();

    public async Task OnGetAsync()
    {
        Restaurants = await _db.Restaurants
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetToggleHighlightAsync(int id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant != null)
        {
            restaurant.IsHighlighted = !restaurant.IsHighlighted;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
