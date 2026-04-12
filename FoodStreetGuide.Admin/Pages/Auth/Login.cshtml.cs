using FoodStreetGuide.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetGuide.Admin.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<AdminUser> _signInManager;
    private readonly UserManager<AdminUser> _userManager;

    public LoginModel(SignInManager<AdminUser> signInManager, UserManager<AdminUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToPage("/Index");
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
