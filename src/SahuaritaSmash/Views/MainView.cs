// The app's one screen. For now (the first step of the move from App Hub) it shows two computer players fighting on
// the mesa with plain shapes, so the match engine can be seen running on Windows and Android; the real pictures,
// menus, keys, touch controls and sounds come next (docs/PLAN.md).
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClassicGames;

namespace SahuaritaSmash.Views;

public sealed class MainView : Control
{
    public BrawlSim Sim { get; private set; }
    bool attached;
    readonly BrawlBot[] bots;
    readonly BrInput[] inputs;
    readonly Stopwatch clock = Stopwatch.StartNew();
    double done;

    public MainView()
    {
        Sim = new BrawlSim(BrawlData.Stages[0], new[] { BrawlData.Fighters[0], BrawlData.Fighters[4] }, 3, 0, 7);
        bots = new[] { new BrawlBot(0, 3), new BrawlBot(1, 3) };
        inputs = new BrInput[2];
        ClipToBounds = true;
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
        int steps = 0;
        while (done + 1 / 60.0 <= now && steps < 5) { Step(); done += 1 / 60.0; steps++; }
        if (steps == 5) done = now;
        InvalidateVisual();
        if (attached) TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Frame);
    }

    /// <summary>One 60 Hz step; a finished match starts again.</summary>
    public void Step()
    {
        if (Sim.Over && Sim.OverT > 180) { RestartMatch(); return; }
        for (int i = 0; i < bots.Length; i++) inputs[i] = bots[i].Think(Sim);
        Sim.Step(inputs);
    }

    void RestartMatch()
    {
        // a new match on the next stage, with the next two fighters
        int stage = (BrawlData.Stages.IndexOf(Sim.Stage) + 1) % BrawlData.Stages.Count;
        Sim = new BrawlSim(BrawlData.Stages[stage], new[] { BrawlData.Fighters[(stage * 3) % 10], BrawlData.Fighters[(stage * 3 + 5) % 10] }, 3, 0, (uint)(7 + stage));
    }

    static IBrush B(uint argb) => new SolidColorBrush(Color.FromUInt32(argb));

    public override void Render(DrawingContext dc)
    {
        var sim = Sim;
        var st = sim.Stage;
        Rect r = new Rect(Bounds.Size);
        dc.FillRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromUInt32(st.Sky1), 0), new GradientStop(Color.FromUInt32(st.Sky2), 1) },
        }, r);
        // world: x -760..760, y -260..520 (y up), fitted to the view
        double scale = Math.Min(r.Width / 1520, r.Height / 780);
        double ox = r.Width / 2, oy = r.Height / 2 + 130 * scale;
        Point P(double x, double y) => new Point(ox + x * scale, oy - y * scale);
        foreach (BPlat p in st.Plats)
        {
            var top = P(p.L, p.Top);
            double h = (p.Solid ? Math.Min(p.Thick, 120) : 10) * scale;
            dc.FillRectangle(B(p.Solid ? st.Ground : st.Accent), new Rect(top.X, top.Y, (p.R - p.L) * scale, h));
            dc.FillRectangle(B(st.GroundTop), new Rect(top.X, top.Y, (p.R - p.L) * scale, 4 * scale));
        }
        var tf = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        for (int i = 0; i < sim.F.Length; i++)
        {
            BFighter f = sim.F[i];
            if (f.Out) continue;
            var feet = P(f.X, f.Y);
            var body = new Rect(feet.X - f.HalfW * scale, feet.Y - f.H * scale, f.HalfW * 2 * scale, f.H * scale);
            dc.DrawRectangle(B(f.D.Main), new Pen(B(0xFF101010), 2), body, 10 * scale, 10 * scale);
            dc.DrawEllipse(B(f.D.Skin), null, new Point(body.Center.X + f.Face * f.HalfW * 0.4 * scale, body.Top + 16 * scale), 10 * scale, 10 * scale);
            var label = new FormattedText(f.D.Name + "  " + f.Percent + "%  " + new string('*', Math.Max(0, f.Stocks)), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 18, Brushes.White);
            dc.DrawText(label, new Point(20 + i * r.Width / 2, r.Height - 40));
        }
        var title = new FormattedText("Sahuarita Smash! (Sauce Edition) - moving from App Hub: computer players on " + st.Name, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 16, Brushes.White);
        dc.DrawText(title, new Point(20, 14));
    }
}
