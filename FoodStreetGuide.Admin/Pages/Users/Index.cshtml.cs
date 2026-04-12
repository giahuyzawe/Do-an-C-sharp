using FoodStreetGuide.Admin.Data;
using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetGuide.Admin.Pages.Users;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<AppUser> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _db.AppUsers
            .OrderByDescending(u => u.JoinDate)
            .Take(50)
            .ToListAsync();
    }
}
