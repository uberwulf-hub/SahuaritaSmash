// The app's one screen: Sahuarita Smash! itself (App Hub's BrawlGame) in a GameView, with its scores and keys in Data's
// folder. Quit (or Esc) on the title closes the app; on Android the Back button also leaves from the title.
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ClassicGames;
using SahuaritaSmash.Services;

namespace SahuaritaSmash.Views
{
    public sealed class MainView : Panel
    {
        public readonly BrawlGame Game;
        public readonly GameView View = new GameView();

        public MainView()
        {
            Game = NewGame();
            View.Game = Game;
            View.ExitRequested += (s, e) =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow?.Close();
                else if (App.QuitApp != null) App.QuitApp();
                else Game.Begin();
            };
            App.Backgrounded += () => Avalonia.Threading.Dispatcher.UIThread.Post(View.AppPaused);
            Children.Add(View);
            App.EnsureSound();
            Game.Begin();
        }

        /// <summary>The game with this app's scorebook (keys are kept next to it, as in App Hub).</summary>
        public static BrawlGame NewGame()
        {
            return new BrawlGame { Scores = new ScoreBook(Data.Scores), Player = "Player" };
        }
    }
}
