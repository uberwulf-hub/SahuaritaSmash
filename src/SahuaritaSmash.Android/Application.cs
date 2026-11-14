using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace SahuaritaSmash.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            // the sound, before the app starts (the app starts here, before the activity exists): an AudioTrack, and
            // the recorded songs from the app's assets, decoded by the phone
            SahuaritaSmash.App.MakeAudioOut = () => new TrackOut();
            try { ClassicGames.BrawlMusic.Source = new AssetMusic(global::Android.App.Application.Context.Assets); } catch (System.Exception) { }
            return base.CustomizeAppBuilder(builder).WithInterFont();
        }
    }
}
