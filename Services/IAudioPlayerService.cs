namespace FoodStreetGuide.Services;

public interface IAudioPlayerService
{
    Task PlayAsync(string audioFile);
    Task PlayFromUrlAsync(string url);
    Task StopAsync();
    bool IsPlaying { get; }
}
