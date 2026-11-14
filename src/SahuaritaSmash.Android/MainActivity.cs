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
    }

    protected override void OnPause()
    {
        App.GoneToBackground();
        base.OnPause();
    }
}
