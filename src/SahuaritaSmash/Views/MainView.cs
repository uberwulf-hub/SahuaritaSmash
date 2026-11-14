// The app's one screen. For now (while the game moves over from App Hub, docs/PLAN.md) it shows four computer players
// fighting, stage after stage, with the game's real pictures (BrawlView); the menus, keys, touch controls and sounds
// come next. The game draws a 1280 x 720 picture, scaled to fit the window with bars at the sides or top.
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClassicGames;

namespace SahuaritaSmash.Views;

public sealed class MainView : Control
{
    public const double GameW = 1280, GameH = 720;
    /// <summary>App Hub's BrawlGame.BotLevels (Easy, Normal, Hard, Champion), until the game itself moves over.</summary>
    static readonly int[] BotLevels = { 1, 3, 6, 9 };
    public BrawlSim Sim { get; private set; }
    public readonly BrawlView View = new BrawlView();
    BrawlBot[] bots;
    BrInput[] inputs;
    string[] names;
    readonly Stopwatch clock = Stopwatch.StartNew();
    double done, t;
    int match;
    bool attached;

    public MainView()
    {
        ClipToBounds = true;
        NewMatch();
    }

    /// <summary>The next showcase match: the next stage, four fighters in turn, computer players of mixed levels.</summary>
    public void NewMatch()
    {
        var stage = BrawlData.Stages[match % BrawlData.Stages.Count];
        var fighters = Enumerable.Range(0, 4).Select(i => BrawlData.Fighters[(match * 3 + i * 2 + i / 2) % BrawlData.Fighters.Count]).ToList();
        Sim = new BrawlSim(stage, fighters, 2, 0, (uint)(11 + match * 7));
        bots = Enumerable.Range(0, 4).Select(i => new BrawlBot(i, BotLevels[(i + match) % 4])).ToArray();
        inputs = new BrInput[4];
        names = fighters.Select(f => f.Name).ToArray();
        View.Fx.Clear(); View.Follow(Sim, true);
        match++;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attached = true;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Frame);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { attached = false; base.OnDetachedFromVisualTree(e); }

    void Frame(TimeSpan _)
    {
        double now = clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.1, now - t);
        Tick(dt);
        InvalidateVisual();
        if (attached) TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Frame);
    }

    /// <summary>Moves the match on by dt seconds (fixed 60 Hz steps, as App Hub's BrawlGame.Tick).</summary>
    public void Tick(double dt)
    {
        t += dt; done += dt;
        int steps = 0;
        while (done >= 1.0 / 60 && steps < 4)
        {
            done -= 1.0 / 60; steps++;
            if (Sim.Over && Sim.OverT > 240) { NewMatch(); break; }
            for (int i = 0; i < bots.Length; i++) inputs[i] = bots[i].Think(Sim);
            Sim.Step(inputs);
            View.Take(Sim);
        }
        if (steps == 4) done = 0;
        View.Follow(Sim, false);
        View.StepFx(Sim, dt);
    }

    /// <summary>The game's picture at 1280 x 720 (snapshots draw this into a bitmap).</summary>
    public void DrawGame(ClassicGames.DrawingContext dc)
    {
        View.W = GameW; View.H = GameH;
        View.Draw(dc, Sim, t);
        View.DrawPanels(dc, Sim, names, t);
    }

    public override void Render(Avalonia.Media.DrawingContext context)
    {
        Rect r = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Black, r);
        double k = Math.Min(r.Width / GameW, r.Height / GameH);
        if (k <= 0) return;
        double ox = (r.Width - GameW * k) / 2, oy = (r.Height - GameH * k) / 2;
        using (context.PushClip(new Rect(ox, oy, GameW * k, GameH * k)))
        using (context.PushTransform(Matrix.CreateScale(k, k) * Matrix.CreateTranslation(ox, oy)))
        {
            var dc = new ClassicGames.DrawingContext(context);
            DrawGame(dc);
            dc.Dispose();
        }
    }
}
