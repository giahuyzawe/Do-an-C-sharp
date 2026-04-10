using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FoodStreetGuide.Platforms.Android;

namespace FoodStreetGuide;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnDestroy()
    {
        // Force stop tracking service when app is killed
        var intent = new Intent(this, typeof(LocationTrackingService));
        StopService(intent);
        
        base.OnDestroy();
    }
}
