using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetGuide.Admin.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly SignInManager<AdminUser> _signInManager;

    public LogoutModel(SignInManager<AdminUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Auth/Login");
    }
}
