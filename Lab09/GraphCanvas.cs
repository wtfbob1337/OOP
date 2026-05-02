using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Lab09;

internal sealed class GraphCanvas : Control
{
    private readonly Font labelFont = new("Segoe UI", 9f);
    private readonly Font titleFont = new("Segoe UI Semibold", 11f, FontStyle.Bold);
    private List<GraphPoint> points = new();
    private GraphSettings settings = new();
    private Rectangle plotArea;
    private double minX = -1;
    private double maxX = 1;
    private double minY = -1;
    private double maxY = 1;

    public GraphCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        ResizeRedraw = true;
    }

    public IReadOnlyList<GraphPoint> Points => points;

    public void Build(GraphSettings nextSettings)
    {
        settings = nextSettings;
        points = CalculatePoints(nextSettings);
        UpdateGraphBounds();
        Invalidate();
    }

    public void SaveImage(string path)
    {
        using var bitmap = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
        DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        bitmap.Save(path, ImageFormat.Png);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(Color.FromArgb(255, 253, 247));
        plotArea = new Rectangle(76, 38, Math.Max(10, Width - 112), Math.Max(10, Height - 100));
        DrawBackground(e.Graphics);
        DrawGrid(e.Graphics);
        DrawAxes(e.Graphics);
        DrawCurve(e.Graphics);
        DrawHeader(e.Graphics);
    }

    private static List<GraphPoint> CalculatePoints(GraphSettings graphSettings)
    {
        var result = new List<GraphPoint>();
        var start = Math.Min(graphSettings.TMin, graphSettings.TMax);
        var end = Math.Max(graphSettings.TMin, graphSettings.TMax);
        var step = Math.Abs(graphSettings.Step);

        if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
        {
            step = Math.Max(0.01, (end - start) / 900);
        }

        if (Math.Abs(end - start) < 1e-12)
        {
            end = start + step;
        }

        var guard = 0;

        for (var t = start; t <= end + step * 0.5 && guard < 250000; t += step, guard++)
        {
            var cos = Math.Cos(t);
            var sin = Math.Sin(t);
            var x = graphSettings.A * cos * (cos + graphSettings.B);
            var y = sin * (sin + graphSettings.B);
            result.Add(new GraphPoint(t, x, y));
        }

        return result;
    }

    private void UpdateGraphBounds()
    {
        if (points.Count == 0)
        {
            minX = -1;
            maxX = 1;
            minY = -1;
            maxY = 1;
            return;
        }

        minX = Math.Min(0, points.Min(point => point.X));
        maxX = Math.Max(0, points.Max(point => point.X));
        minY = Math.Min(0, points.Min(point => point.Y));
        maxY = Math.Max(0, points.Max(point => point.Y));
        ExpandRange(ref minX, ref maxX);
        ExpandRange(ref minY, ref maxY);
    }

    private static void ExpandRange(ref double min, ref double max)
    {
        if (Math.Abs(max - min) < 1e-9)
        {
            min -= 1;
            max += 1;
            return;
        }

        var margin = (max - min) * 0.14;
        min -= margin;
        max += margin;
    }

    private void DrawBackground(Graphics graphics)
    {
        using var fill = new SolidBrush(Color.FromArgb(255, 253, 247));
        using var border = new Pen(Color.FromArgb(210, 202, 188));
        graphics.FillRectangle(fill, plotArea);
        graphics.DrawRectangle(border, plotArea);
    }

    private void DrawGrid(Graphics graphics)
    {
        var xStep = NiceStep((maxX - minX) / 10);
        var yStep = NiceStep((maxY - minY) / 8);

        using var gridPen = new Pen(Color.FromArgb(229, 222, 210));
        using var textBrush = new SolidBrush(Color.FromArgb(88, 84, 78));

        for (var x = Math.Ceiling(minX / xStep) * xStep; x <= maxX; x += xStep)
        {
            var sx = ToScreenX(x);
            graphics.DrawLine(gridPen, sx, plotArea.Top, sx, plotArea.Bottom);
            graphics.DrawString(FormatNumber(x), labelFont, textBrush, sx - 18, plotArea.Bottom + 8);
        }

        for (var y = Math.Ceiling(minY / yStep) * yStep; y <= maxY; y += yStep)
        {
            var sy = ToScreenY(y);
            graphics.DrawLine(gridPen, plotArea.Left, sy, plotArea.Right, sy);
            graphics.DrawString(FormatNumber(y), labelFont, textBrush, 12, sy - 8);
        }
    }

    private void DrawAxes(Graphics graphics)
    {
        using var axisPen = new Pen(Color.FromArgb(31, 37, 35), 2f);
        using var arrowBrush = new SolidBrush(Color.FromArgb(31, 37, 35));
        using var textBrush = new SolidBrush(Color.FromArgb(31, 37, 35));
        var xAxisY = ToScreenY(0);
        var yAxisX = ToScreenX(0);

        if (xAxisY >= plotArea.Top && xAxisY <= plotArea.Bottom)
        {
            graphics.DrawLine(axisPen, plotArea.Left, xAxisY, plotArea.Right, xAxisY);
            DrawArrow(graphics, arrowBrush, new PointF(plotArea.Right, xAxisY), true);
            graphics.DrawString("x", titleFont, textBrush, plotArea.Right - 14, xAxisY - 24);
        }

        if (yAxisX >= plotArea.Left && yAxisX <= plotArea.Right)
        {
            graphics.DrawLine(axisPen, yAxisX, plotArea.Bottom, yAxisX, plotArea.Top);
            DrawArrow(graphics, arrowBrush, new PointF(yAxisX, plotArea.Top), false);
            graphics.DrawString("y", titleFont, textBrush, yAxisX + 8, plotArea.Top + 2);
        }
    }

    private static void DrawArrow(Graphics graphics, Brush brush, PointF tip, bool horizontal)
    {
        PointF[] polygon = horizontal
            ? new[] { tip, new PointF(tip.X - 9, tip.Y - 5), new PointF(tip.X - 9, tip.Y + 5) }
            : new[] { tip, new PointF(tip.X - 5, tip.Y + 9), new PointF(tip.X + 5, tip.Y + 9) };
        graphics.FillPolygon(brush, polygon);
    }

    private void DrawCurve(Graphics graphics)
    {
        if (points.Count < 2)
        {
            DrawEmptyMessage(graphics);
            return;
        }

        var screenPoints = points.Select(point => new PointF(ToScreenX(point.X), ToScreenY(point.Y))).ToArray();

        if (settings.FillArea && screenPoints.Length > 2)
        {
            using var areaBrush = new SolidBrush(Color.FromArgb(52, 205, 118, 86));
            graphics.FillPolygon(areaBrush, screenPoints);
        }

        using var curvePen = new Pen(Color.FromArgb(0, 118, 130), 3.2f)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLines(curvePen, screenPoints);

        if (settings.ShowPoints)
        {
            using var brush = new SolidBrush(Color.FromArgb(181, 78, 60));

            foreach (var point in screenPoints.Where((_, index) => index % Math.Max(1, screenPoints.Length / 90) == 0))
            {
                graphics.FillEllipse(brush, point.X - 2.6f, point.Y - 2.6f, 5.2f, 5.2f);
            }
        }
    }

    private void DrawHeader(Graphics graphics)
    {
        using var titleBrush = new SolidBrush(Color.FromArgb(31, 37, 35));
        using var textBrush = new SolidBrush(Color.FromArgb(86, 91, 87));
        graphics.DrawString("Варіант 10: x = a cos(t)(cos(t)+b), y = sin(t)(sin(t)+b)", titleFont, titleBrush, plotArea.Left, 10);
        graphics.DrawString($"a = {FormatNumber(settings.A)}, b = {FormatNumber(settings.B)}, t [{FormatNumber(settings.TMin)}; {FormatNumber(settings.TMax)}], точок: {points.Count}", labelFont, textBrush, plotArea.Left + 438, 13);
    }

    private void DrawEmptyMessage(Graphics graphics)
    {
        using var brush = new SolidBrush(Color.FromArgb(160, 70, 55));
        var text = "Немає точок для побудови";
        var size = graphics.MeasureString(text, titleFont);
        graphics.DrawString(text, titleFont, brush, plotArea.Left + (plotArea.Width - size.Width) / 2, plotArea.Top + (plotArea.Height - size.Height) / 2);
    }

    private float ToScreenX(double x)
    {
        return (float)(plotArea.Left + (x - minX) / (maxX - minX) * plotArea.Width);
    }

    private float ToScreenY(double y)
    {
        return (float)(plotArea.Bottom - (y - minY) / (maxY - minY) * plotArea.Height);
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0 || double.IsNaN(raw) || double.IsInfinity(raw))
        {
            return 1;
        }

        var power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var fraction = raw / power;
        var nice = fraction switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10
        };
        return nice * power;
    }

    private static string FormatNumber(double value)
    {
        if (Math.Abs(value) < 1e-9)
        {
            value = 0;
        }

        return Math.Abs(value) >= 1000 || Math.Abs(value) < 0.01 && value != 0
            ? value.ToString("0.##E+0")
            : value.ToString("0.##");
    }
}
