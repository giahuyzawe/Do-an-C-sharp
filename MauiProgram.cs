using Microsoft.Extensions.Logging;
using FoodStreetGuide.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetGuide;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<IWebAdminService, WebAdminService>();
		builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
		builder.Services.AddSingleton<ISavedPOIService, SavedPOIService>();

#if ANDROID
		// Register platform-specific services BEFORE services that depend on them
		builder.Services.AddSingleton<ISettingsService, Platforms.Android.SettingsServiceAndroid>();
		builder.Services.AddSingleton<IGeofenceEngine, GeofenceEngine>(); // Depends on ISettingsService
		builder.Services.AddSingleton<ITTSService, Platforms.Android.TTSServiceAndroid>();
		builder.Services.AddSingleton<INarrationEngine, Platforms.Android.NarrationEngineAndroid>();
		builder.Services.AddSingleton<IAudioPlayerService, Platforms.Android.AudioPlayerServiceAndroid>();
		builder.Services.AddSingleton<ILocationService, Platforms.Android.LocationServiceAndroid>();
#endif

		return builder.Build();
	}
	
}
