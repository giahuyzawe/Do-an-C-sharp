using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages.POIs;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private const int PageSize = 20;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<POI> POIs { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; } = "active";

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "newest";

    public SelectList CategoryOptions { get; set; } = new SelectList(new[] { "landmark", "restaurant", "cafe", "market", "temple", "bridge", "park" });
    public SelectList StatusOptions { get; set; } = new SelectList(new[] { "active", "inactive", "draft" });
    public SelectList SortOptions { get; set; } = new SelectList(new[] { "newest", "oldest", "name", "priority" });

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = page;

        var query = _db.POIs.AsQueryable();

        if (!string.IsNullOrEmpty(Search))
            query = query.Where(p => p.NameVi.Contains(Search) || (p.NameEn != null && p.NameEn.Contains(Search)));

        if (!string.IsNullOrEmpty(Category))
            query = query.Where(p => p.Category == Category);

        if (!string.IsNullOrEmpty(Status))
            query = query.Where(p => p.Status == Status);

        query = Sort switch
        {
            "oldest" => query.OrderBy(p => p.CreatedAt),
            "name" => query.OrderBy(p => p.NameVi),
            "priority" => query.OrderByDescending(p => p.Priority),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        TotalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

        POIs = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var poi = await _db.POIs.FindAsync(id);
        if (poi != null)
        {
            _db.POIs.Remove(poi);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
