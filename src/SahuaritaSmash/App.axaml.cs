using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
            desktop.MainWindow = MakeWindow();
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = () => new MainView();
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = new MainView();
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>The Windows window: full screen with no title bar (the game has its own Back and Quit; Andrew,
    /// 2026-11-15). F11 or Alt+Enter switches to an ordinary window and back.</summary>
    static Window MakeWindow()
    {
        var w = new Window
        {
            Title = "Sahuarita Smash!",
            Width = 1280, Height = 720, MinWidth = 640, MinHeight = 360,
            Background = Avalonia.Media.Brushes.Black,
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://SahuaritaSmash/Assets/SahuaritaSmash.ico"))),
            Content = new MainView(),
            WindowState = WindowState.FullScreen,
        };
        w.AddHandler(InputElement.KeyDownEvent, (sender, e) =>
        {
            bool toggle = e.Key == Avalonia.Input.Key.F11 || (e.Key == Avalonia.Input.Key.Enter && (e.KeyModifiers & Avalonia.Input.KeyModifiers.Alt) != 0);
            if (!toggle) return;
            w.WindowState = w.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        return w;
    }

    /// <summary>The app went to the background (Android): the game pauses, as when a window goes to the back.</summary>
    public static event System.Action Backgrounded;
    public static void GoneToBackground() { var h = Backgrounded; if (h != null) h(); }

    /// <summary>The platform's sound output (Windows: waveOut; Android: AudioTrack). Null: silent.</summary>
    public static System.Func<ClassicGames.IAudioOut> MakeAudioOut;

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
        EnsureSound();
    }

    /// <summary>Starts the sound on the platform's output once it is known (Program.cs and Android's Application.cs
    /// set MakeAudioOut before the app starts; the game's screen asks again when it opens, so the order can't leave
    /// the game silent: on 0.1.0 Android started the app before its activity had set the output). Never in the
    /// checks.</summary>
    public static void EnsureSound()
    {
        if (Testing || ClassicGames.Audio.Live || MakeAudioOut == null) return;
        try { ClassicGames.Audio.Start(MakeAudioOut()); ClassicGames.BrawlMusic.PreloadAll(); }
        catch (System.Exception ex) { ClassicGames.CrashLog.Write("starting the sound", ex, false); }
    }
}
