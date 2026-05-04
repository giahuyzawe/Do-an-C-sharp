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
            // Delay to ensure TabBar items are loaded
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                UpdateTabTitles();
            });
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateTabTitles();
    }

    private void UpdateTabTitles()
    {
        // Update tab titles based on current language
        if (_localizationService == null) return;

        if (Items.Count > 0 && Items[0] is TabBar tabBar)
        {
            var titles = new[] { "Tab_Map", "Tab_Discover", "Tab_Saved", "Tab_Settings" };
            
            for (int i = 0; i < tabBar.Items.Count && i < titles.Length; i++)
            {
                var translatedTitle = _localizationService.GetString(titles[i]);
                if (!string.IsNullOrEmpty(translatedTitle))
                {
                    tabBar.Items[i].Title = translatedTitle;
                }
            }
        }
    }

    public void RefreshTabTitles()
    {
        UpdateTabTitles();
    }
}
