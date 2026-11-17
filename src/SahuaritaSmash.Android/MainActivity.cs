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
    global::Android.Net.Wifi.WifiManager.MulticastLock multicast;

    protected override void OnCreate(global::Android.OS.Bundle savedInstanceState)
    {
        App.QuitApp = () => Finish();
        App.Haptic = Buzz;   // a short buzz on hits (Touch controls > Vibration)
        // the phone's volume buttons turn the game up and down (not the ringer), even between sounds
        VolumeControlStream = global::Android.Media.Stream.Music;
        // hosting on the home network: hear the others' "who's hosting?" (Android drops broadcasts without this)
        ClassicGames.LanTransport.HoldBroadcasts = hold =>
        {
            try
            {
                if (hold)
                {
                    var wifi = (global::Android.Net.Wifi.WifiManager)ApplicationContext.GetSystemService(WifiService);
                    multicast = wifi.CreateMulticastLock("SahuaritaSmash");
                    multicast.SetReferenceCounted(false);
                    multicast.Acquire();
                }
                else if (multicast != null) { multicast.Release(); multicast = null; }
            }
            catch (System.Exception ex) { ClassicGames.CrashLog.Write("the home network's broadcasts", ex, false); }
        };
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

    /// <summary>A short buzz of the phone (API 26+: VibrationEffect; Android 12+ through VibratorManager). Phones
    /// without a vibrator, or anything going wrong, simply don't buzz.</summary>
    void Buzz(int ms)
    {
        try
        {
            global::Android.OS.Vibrator v = null;
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
            {
                var vm = GetSystemService(VibratorManagerService) as global::Android.OS.VibratorManager;
                if (vm != null) v = vm.DefaultVibrator;
            }
            else
            {
#pragma warning disable CA1422
                v = GetSystemService(VibratorService) as global::Android.OS.Vibrator;
#pragma warning restore CA1422
            }
            if (v == null || !v.HasVibrator) return;
            v.Vibrate(global::Android.OS.VibrationEffect.CreateOneShot(ms, global::Android.OS.VibrationEffect.DefaultAmplitude));
        }
        catch (System.Exception ex) { ClassicGames.CrashLog.Write("the phone's buzz", ex, false); }
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) FullScreen();
    }

    /// <summary>The volume buttons always change the game's volume (music and sounds), with Android's own volume bar.
    /// They are handled here, before the game's screen sees any key: Avalonia's view took them as key presses, so
    /// in 0.3.0 the buttons did nothing in the game (Andrew).</summary>
    public override bool DispatchKeyEvent(global::Android.Views.KeyEvent e)
    {
        var code = e.KeyCode;
        if (code == global::Android.Views.Keycode.VolumeUp || code == global::Android.Views.Keycode.VolumeDown || code == global::Android.Views.Keycode.VolumeMute)
        {
            if (e.Action == global::Android.Views.KeyEventActions.Down)
            {
                try
                {
                    var audio = (global::Android.Media.AudioManager)GetSystemService(AudioService);
                    var dir = code == global::Android.Views.Keycode.VolumeUp ? global::Android.Media.Adjust.Raise
                        : code == global::Android.Views.Keycode.VolumeDown ? global::Android.Media.Adjust.Lower : global::Android.Media.Adjust.ToggleMute;
                    audio.AdjustStreamVolume(global::Android.Media.Stream.Music, dir, global::Android.Media.VolumeNotificationFlags.ShowUi);
                }
                catch (System.Exception ex) { ClassicGames.CrashLog.Write("the volume buttons", ex, false); }
            }
            return true;
        }
        return base.DispatchKeyEvent(e);
    }

    protected override void OnPause()
    {
        App.GoneToBackground();
        base.OnPause();
    }
}
