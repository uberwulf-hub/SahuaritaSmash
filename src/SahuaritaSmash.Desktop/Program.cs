using Avalonia;

namespace SahuaritaSmash.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // the sound: waveOut, and Andrew's recorded songs next to the exe (Music\Brawl), decoded by Media Foundation
        App.MakeAudioOut = () => new WaveOut();
        ClassicGames.BrawlMusic.Folder = Path.Combine(AppContext.BaseDirectory, "Music", "Brawl");
        try { BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
        finally { ClassicGames.Audio.Shutdown(); }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
