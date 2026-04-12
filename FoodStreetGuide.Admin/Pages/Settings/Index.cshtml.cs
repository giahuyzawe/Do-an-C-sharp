using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages.Settings;

[Authorize(Roles = "SuperAdmin,Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<AppSetting> AppSettings { get; set; } = new();

    public async Task OnGetAsync()
    {
        AppSettings = await _db.AppSettings.ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(Dictionary<string, string> settings)
    {
        foreach (var item in settings)
        {
            var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
            if (setting != null)
            {
                setting.Value = item.Value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
