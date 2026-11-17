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
        // a game controller's buttons and d-pad (only from a gamepad or joystick: the phone's own keys and Back are
        // left alone) go to the shared controller reader, not to the game's screen as keys
        if (FromPad(e.Source) && ClassicGames.KartPadInput.IsPadKey((int)code))
        {
            try
            {
                WatchPads();
                if (e.Action == global::Android.Views.KeyEventActions.Down) ClassicGames.KartPads.Input.Key(e.DeviceId, (int)code, true);
                else if (e.Action == global::Android.Views.KeyEventActions.Up) ClassicGames.KartPads.Input.Key(e.DeviceId, (int)code, false);
            }
            catch (System.Exception ex) { ClassicGames.CrashLog.Write("a game controller's button", ex, false); }
            return true;
        }
        return base.DispatchKeyEvent(e);
    }

    /// <summary>A game controller's sticks, triggers and d-pad hat (Bluetooth or USB gamepads): every axis the shared
    /// reader knows, read on each joystick event (PadInput.cs turns them into controller 1-4).</summary>
    public override bool DispatchGenericMotionEvent(global::Android.Views.MotionEvent e)
    {
        if (e != null && FromPad(e.Source) && e.Action == global::Android.Views.MotionEventActions.Move)
        {
            try
            {
                WatchPads();
                foreach (int axis in ClassicGames.KartPadInput.Axes)
                    ClassicGames.KartPads.Input.Axis(e.DeviceId, axis, e.GetAxisValue((global::Android.Views.Axis)axis));
            }
            catch (System.Exception ex) { ClassicGames.CrashLog.Write("a game controller's sticks", ex, false); }
            return true;
        }
        return base.DispatchGenericMotionEvent(e);
    }

    static bool FromPad(global::Android.Views.InputSourceType source)
    {
        return (source & global::Android.Views.InputSourceType.Gamepad) == global::Android.Views.InputSourceType.Gamepad
            || (source & global::Android.Views.InputSourceType.Joystick) == global::Android.Views.InputSourceType.Joystick;
    }

    /// <summary>A controller that is switched off or unpaired frees its slot: the reader asks about once a second
    /// whether each controller it knows still exists.</summary>
    static void WatchPads()
    {
        if (ClassicGames.KartPads.Input.Exists == null)
            ClassicGames.KartPads.Input.Exists = id => global::Android.Views.InputDevice.GetDevice(id) != null;
    }

    protected override void OnPause()
    {
        App.GoneToBackground();
        base.OnPause();
    }
}
