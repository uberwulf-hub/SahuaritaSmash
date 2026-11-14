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
}
