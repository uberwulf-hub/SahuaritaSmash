// WPF's drawing calls on top of Avalonia's, so the pictures from App Hub's Classic Games (Engine\Brawl*.cs there) keep
// their code: DrawingContext with Push... / Pop(), StreamGeometry with WPF's figure calls (BeginFigure(p, filled,
// closed), LineTo(p, stroked, smooth), ...), the transforms with WPF's constructors, Freeze(). Points, rects, matrices,
// colours, brushes and pens are Avalonia's own (they match WPF's closely). These types live in the namespace
// ClassicGames, so in the ported files they win over Avalonia's of the same name.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using A = Avalonia.Media;

namespace ClassicGames
{
    /// <summary>WPF's DrawingContext: every Push... is undone by one Pop().</summary>
    public sealed class DrawingContext : IDisposable
    {
        public readonly A.DrawingContext Ctx;
        readonly Stack<IDisposable> pushed = new Stack<IDisposable>();
        readonly IDisposable owner;

        public DrawingContext(A.DrawingContext ctx) { Ctx = ctx; }
        /// <summary>One that disposes its Avalonia context at the end (drawing into a bitmap).</summary>
        public DrawingContext(A.DrawingContext ctx, bool owns) { Ctx = ctx; if (owns) owner = ctx; }

        public void DrawRectangle(IBrush brush, IPen pen, Rect r) { if (r.Width < 0 || r.Height < 0) return; Ctx.DrawRectangle(brush, pen, r); }
        public void DrawRoundedRectangle(IBrush brush, IPen pen, Rect r, double rx, double ry) { if (r.Width < 0 || r.Height < 0) return; Ctx.DrawRectangle(brush, pen, r, rx, ry); }
        public void DrawEllipse(IBrush brush, IPen pen, Point center, double rx, double ry) { Ctx.DrawEllipse(brush, pen, center, Math.Abs(rx), Math.Abs(ry)); }
        public void DrawLine(IPen pen, Point a, Point b) { if (pen != null) Ctx.DrawLine(pen, a, b); }
        public void DrawGeometry(IBrush brush, IPen pen, Geometry g) { if (g != null) Ctx.DrawGeometry(brush, pen, g); }
        public void DrawGeometry(IBrush brush, IPen pen, StreamGeometry g) { if (g != null) Ctx.DrawGeometry(brush, pen, g.G); }
        public void DrawText(FormattedText t, Point p) { Ctx.DrawText(t, p); }
        public void DrawImage(IImage image, Rect r) { if (image != null && r.Width > 0 && r.Height > 0) Ctx.DrawImage(image, r); }

        public void PushTransform(Transform t) { pushed.Push(Ctx.PushTransform(t.Value)); }
        public void PushOpacity(double opacity) { pushed.Push(Ctx.PushOpacity(Math.Max(0, Math.Min(1, opacity)))); }
        public void PushClip(Geometry g) { pushed.Push(Ctx.PushGeometryClip(g)); }
        public void PushClip(StreamGeometry g) { pushed.Push(Ctx.PushGeometryClip(g.G)); }
        public void Pop() { if (pushed.Count > 0) pushed.Pop().Dispose(); }

        public void Dispose()
        {
            while (pushed.Count > 0) pushed.Pop().Dispose();
            if (owner != null) owner.Dispose();
        }
    }

    /// <summary>WPF's StreamGeometry: figures are closed (or left open) as WPF's BeginFigure said.</summary>
    public sealed class StreamGeometry
    {
        public readonly A.StreamGeometry G = new A.StreamGeometry();
        public StreamGeometryContext Open() { return new StreamGeometryContext(G.Open()); }
        public static implicit operator Geometry(StreamGeometry g) { return g == null ? null : g.G; }
        public Rect Bounds { get { return G.Bounds; } }
    }

    public sealed class StreamGeometryContext : IDisposable
    {
        readonly A.StreamGeometryContext c;
        bool open, closeIt;
        public StreamGeometryContext(A.StreamGeometryContext c) { this.c = c; }

        void End() { if (open) c.EndFigure(closeIt); open = false; }
        public void BeginFigure(Point p, bool filled, bool closed) { End(); c.BeginFigure(p, filled); open = true; closeIt = closed; }
        public void LineTo(Point p, bool stroked, bool smooth) { c.LineTo(p, stroked); }
        public void BezierTo(Point a, Point b, Point e, bool stroked, bool smooth) { c.CubicBezierTo(a, b, e, stroked); }
        public void QuadraticBezierTo(Point ctl, Point e, bool stroked, bool smooth) { c.QuadraticBezierTo(ctl, e, stroked); }
        public void ArcTo(Point p, Size size, double rotation, bool large, SweepDirection sweep, bool stroked, bool smooth) { c.ArcTo(p, size, rotation, large, sweep, stroked); }
        public void Dispose() { End(); c.Dispose(); }
    }

    /// <summary>WPF's transforms (angles in degrees, centres as WPF's constructors take them).</summary>
    public abstract class Transform { public abstract Matrix Value { get; } }

    public sealed class MatrixTransform : Transform
    {
        readonly Matrix m;
        public MatrixTransform(Matrix m) { this.m = m; }
        public override Matrix Value { get { return m; } }
    }

    public sealed class TranslateTransform : Transform
    {
        public double X, Y;
        public TranslateTransform(double x, double y) { X = x; Y = y; }
        public override Matrix Value { get { return Matrix.CreateTranslation(X, Y); } }
    }

    public sealed class ScaleTransform : Transform
    {
        readonly double sx, sy, cx, cy;
        public ScaleTransform(double sx, double sy) : this(sx, sy, 0, 0) { }
        public ScaleTransform(double sx, double sy, double cx, double cy) { this.sx = sx; this.sy = sy; this.cx = cx; this.cy = cy; }
        public override Matrix Value { get { return Matrix.CreateTranslation(-cx, -cy) * Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(cx, cy); } }
    }

    public sealed class RotateTransform : Transform
    {
        readonly double angle, cx, cy;
        public RotateTransform(double angle) : this(angle, 0, 0) { }
        public RotateTransform(double angle, double cx, double cy) { this.angle = angle; this.cx = cx; this.cy = cy; }
        public override Matrix Value { get { return Matrix.CreateTranslation(-cx, -cy) * Matrix.CreateRotation(angle * Math.PI / 180) * Matrix.CreateTranslation(cx, cy); } }
    }

    /// <summary>WPF's Int32Rect (cropping bitmaps).</summary>
    public struct Int32Rect
    {
        public int X, Y, Width, Height;
        public Int32Rect(int x, int y, int w, int h) { X = x; Y = y; Width = w; Height = h; }
        public static implicit operator PixelRect(Int32Rect r) { return new PixelRect(r.X, r.Y, r.Width, r.Height); }
    }

    /// <summary>WPF's RenderCapability: Avalonia always draws with Skia, so the full pictures are always drawn.</summary>
    public static class RenderCapability { public static int Tier { get { return 2 << 16; } } }

    public static class WpfExtensions
    {
        /// <summary>WPF freezes brushes, pens and geometry for speed; Avalonia needs nothing.</summary>
        public static void Freeze(this object o) { }
        /// <summary>WPF's Matrix.Transform(point).</summary>
        public static Point Transform(this Matrix m, Point p) { return p * m; }
    }

    /// <summary>Gradient brushes as WPF makes them (relative to the shape's box).</summary>
    public static class WpfBrush
    {
        public static RelativePoint Rel(double x, double y) { return new RelativePoint(x, y, RelativeUnit.Relative); }
        public static RelativeScalar Rel(double r) { return new RelativeScalar(r, RelativeUnit.Relative); }
        public static Color C(uint c) { return Color.FromArgb((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c); }

        /// <summary>WPF's LinearGradientBrush(start, end, angle): from the top left corner, angle 0 = left to right, 90 = top to bottom.</summary>
        public static LinearGradientBrush Linear(Color a, Color b, double angle)
        {
            double r = angle * Math.PI / 180;
            var g = new LinearGradientBrush { StartPoint = Rel(0, 0), EndPoint = Rel(Math.Cos(r), Math.Sin(r)) };
            g.GradientStops.Add(new GradientStop(a, 0));
            g.GradientStops.Add(new GradientStop(b, 1));
            return g;
        }
    }
}

namespace ClassicGames
{
    /// <summary>Bitmaps made from pixels (WPF's BitmapSource.Create).</summary>
    public static class Pics
    {
        /// <summary>Straight (not premultiplied) BGRA pixels, one uint each (0xAARRGGBB), rows top down.</summary>
        public static Avalonia.Media.Imaging.WriteableBitmap FromBgra(uint[] px, int w, int h)
        {
            var bmp = new Avalonia.Media.Imaging.WriteableBitmap(new Avalonia.PixelSize(w, h), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul);
            using (var fb = bmp.Lock())
            {
                for (int y = 0; y < h; y++)
                    System.Runtime.InteropServices.Marshal.Copy((int[])(object)px, y * w, fb.Address + y * fb.RowBytes, w);
            }
            return bmp;
        }
    }
}

namespace ClassicGames
{
    /// <summary>WPF's DrawingVisual, for drawing off screen (checks and snapshots): RenderOpen() draws into a bitmap
    /// of the given size (WPF's has no size; 1280 x 720 = the game's screen).</summary>
    public sealed class DrawingVisual
    {
        public readonly Avalonia.Media.Imaging.RenderTargetBitmap Bitmap;
        public DrawingVisual() : this(1280, 720) { }
        public DrawingVisual(int w, int h) { Bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(w, h)); }
        public DrawingContext RenderOpen() { return new DrawingContext(Bitmap.CreateDrawingContext(), true); }
    }
}
