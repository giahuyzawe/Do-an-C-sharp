namespace FoodStreetGuide.Services;

public interface ITTSService
{
    Task SpeakAsync(string text, string language = "vi-VN");
    Task StopAsync();
    bool IsSpeaking { get; }
}
