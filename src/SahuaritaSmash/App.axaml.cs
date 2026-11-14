using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SahuaritaSmash.Views;

namespace SahuaritaSmash;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Start();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window
            {
                Title = "Sahuarita Smash!",
                Width = 1280, Height = 720, MinWidth = 640, MinHeight = 360,
                Background = Avalonia.Media.Brushes.Black,
                Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://SahuaritaSmash/Assets/SahuaritaSmash.ico"))),
                Content = new MainView(),
            };
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = () => new MainView();
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = new MainView();
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>How the platform closes the app when the game asks (Quit on the title). Windows: the window closes;
    /// Android sets this (the activity finishes).</summary>
    public static System.Action QuitApp;

    /// <summary>Set by the checks: no saves brought across, no sound, the test folder.</summary>
    public static bool Testing;

    static bool started;
    /// <summary>Once per run: the error log, App Hub's saves (Windows, the first time), the sound.</summary>
    static void Start()
    {
        if (started || Testing) return;
        started = true;
        ClassicGames.CrashLog.Path = SahuaritaSmash.Services.Data.Errors;
        if (System.OperatingSystem.IsWindows())
            SahuaritaSmash.Services.Data.BringAcrossFromAppHub(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ClassicGames"));
        // the recorded songs and the sound output come in the next step (docs/PLAN.md): silent until then
        ClassicGames.BrawlMusic.Enabled = false;
    }
}
