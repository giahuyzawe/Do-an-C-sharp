namespace FoodStreetGuide.Services;

public static class ServiceProviderHelper
{
    public static T? GetService<T>() where T : class
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
