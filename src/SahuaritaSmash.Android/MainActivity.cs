using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace SahuaritaSmash.Android;

// Landscape only: the stage is 16:9
[Activity(
    Label = "Sahuarita Smash! SE",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(global::Android.OS.Bundle savedInstanceState)
    {
        App.QuitApp = () => Finish();
        // the phone's volume buttons turn the game up and down (not the ringer), even between sounds
        VolumeControlStream = global::Android.Media.Stream.Music;
        base.OnCreate(savedInstanceState);
        FullScreen();
    }

    /// <summary>The whole screen for the game: the status and navigation bars hidden (a swipe from the edge shows them
    /// for a moment), drawn under a camera cutout too. Asked again whenever the game comes back to the front.</summary>
    void FullScreen()
    {
        try
        {
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.P)
                Window.Attributes.LayoutInDisplayCutoutMode = global::Android.Views.LayoutInDisplayCutoutMode.ShortEdges;
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, false);
            var c = AndroidX.Core.View.WindowCompat.GetInsetsController(Window, Window.DecorView);
            c.Hide(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            c.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
        catch (System.Exception ex) { ClassicGames.CrashLog.Write("hiding the system bars", ex, false); }
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) FullScreen();
    }

    protected override void OnPause()
    {
        App.GoneToBackground();
        base.OnPause();
    }
}
