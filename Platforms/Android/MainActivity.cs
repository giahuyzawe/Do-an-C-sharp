using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Net;
using FoodStreetGuide.Platforms.Android;

namespace FoodStreetGuide;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    LaunchMode = LaunchMode.SingleTop, 
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

// Deep link intent filter for foodtour:// scheme
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "foodtour",
    DataHost = "qr")]

public class MainActivity : MauiAppCompatActivity
{
    public static string? DeepLinkToken { get; set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Handle deep link
        var intent = Intent;
        if (intent?.Action == Intent.ActionView && intent.Data != null)
        {
            var uri = intent.Data;
            if (uri.Scheme == "foodtour" && uri.Host == "qr")
            {
                DeepLinkToken = uri.LastPathSegment;
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Deep link received: token={DeepLinkToken}");
            }
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        // Handle deep link when app is already running
        if (intent?.Action == Intent.ActionView && intent.Data != null)
        {
            var uri = intent.Data;
            if (uri.Scheme == "foodtour" && uri.Host == "qr")
            {
                DeepLinkToken = uri.LastPathSegment;
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Deep link from running app: token={DeepLinkToken}");
                
                // TODO: Navigate to QR check-in page with token
            }
        }
    }

    protected override void OnDestroy()
    {
        // Force stop tracking service when app is killed
        var intent = new Intent(this, typeof(LocationTrackingService));
        StopService(intent);
        
        base.OnDestroy();
    }
}
