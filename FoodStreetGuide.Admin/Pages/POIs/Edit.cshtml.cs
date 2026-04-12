using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages.POIs;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public EditModel(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [BindProperty]
    public POI POI { get; set; } = new();

    [BindProperty]
    public IFormFile? ImageFile { get; set; }

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

    public SelectList Categories { get; set; } = new SelectList(new[]
    {
        "landmark", "restaurant", "cafe", "bar", "night_market",
        "temple", "church", "bridge", "park", "museum", "shopping"
    });

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id.HasValue)
        {
            POI = await _db.POIs.FindAsync(id.Value);
            if (POI == null) return NotFound();
        }
        else
        {
            POI = new POI { Latitude = 10.762622, Longitude = 106.660172 };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Handle file uploads
        if (ImageFile != null)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "images");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"poi_{DateTime.Now:yyyyMMddHHmmss}_{ImageFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await ImageFile.CopyToAsync(stream);
            POI.ImageUrl = $"/uploads/images/{fileName}";
        }

        if (AudioFile != null)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "audio");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"poi_{DateTime.Now:yyyyMMddHHmmss}_{AudioFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await AudioFile.CopyToAsync(stream);
            POI.AudioUrl = $"/uploads/audio/{fileName}";
        }

        if (POI.Id == 0)
        {
            POI.CreatedAt = DateTime.UtcNow;
            _db.POIs.Add(POI);
        }
        else
        {
            _db.POIs.Update(POI);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
