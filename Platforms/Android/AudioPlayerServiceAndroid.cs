using Android.Media;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.Platforms.Android;

public class AudioPlayerServiceAndroid : IAudioPlayerService
{
    private MediaPlayer? _mediaPlayer;

    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

    public Task PlayAsync(string audioFile)
    {
        StopAsync();

        var context = Platform.CurrentActivity;
        var assetFileDescriptor = context?.Assets?.OpenFd(audioFile);

        _mediaPlayer = new MediaPlayer();
        if (assetFileDescriptor != null)
        {
            _mediaPlayer.SetDataSource(assetFileDescriptor.FileDescriptor, assetFileDescriptor.StartOffset, assetFileDescriptor.Length);
            _mediaPlayer.Prepare();
            _mediaPlayer.Start();
        }

        return Task.CompletedTask;
    }

    public Task PlayFromUrlAsync(string url)
    {
        StopAsync();

        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.SetDataSource(url);
        _mediaPlayer.Prepare();
        _mediaPlayer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Release();
        _mediaPlayer = null;
        return Task.CompletedTask;
    }
}
