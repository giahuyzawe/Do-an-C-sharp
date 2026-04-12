using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class AppShell : Shell
{
    private readonly ILocalizationService? _localizationService;

    public AppShell()
    {
        InitializeComponent();
        
        _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
        
        if (_localizationService != null)
        {
            _localizationService.LanguageChanged += OnLanguageChanged;
            UpdateTabTitles();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateTabTitles();
    }

    private void UpdateTabTitles()
    {
        if (_localizationService == null) return;
        var loc = _localizationService;
        
        // Update tab titles
        if (Items.Count >= 4)
        {
            if (Items[0] is TabBar tabBar && tabBar.Items.Count >= 4)
            {
                tabBar.Items[0].Title = loc.GetString("Tab_Map");
                tabBar.Items[1].Title = loc.GetString("Tab_Discover");
                tabBar.Items[2].Title = loc.GetString("Tab_Saved");
                tabBar.Items[3].Title = loc.GetString("Tab_Settings");
            }
        }
    }
}
