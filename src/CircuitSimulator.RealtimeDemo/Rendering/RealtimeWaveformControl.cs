using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CircuitSimulator.RealtimeDemo.Formatting;
using CircuitSimulator.RealtimeDemo.Models;

namespace CircuitSimulator.RealtimeDemo.Rendering;

/// <summary>Draws rolling selected-component voltage, current, and power lanes.</summary>
public sealed class RealtimeWaveformControl : Control
{
    /// <summary>Identifies the immutable chart snapshot.</summary>
    public static readonly StyledProperty<WaveformChartSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, WaveformChartSnapshot?>(nameof(Snapshot));

    /// <summary>Identifies the chart-grid brush.</summary>
    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(GridBrush), Brushes.DimGray);

    /// <summary>Identifies the voltage trace brush.</summary>
    public static readonly StyledProperty<IBrush> VoltageBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(VoltageBrush), Brushes.CornflowerBlue);

    /// <summary>Identifies the current trace brush.</summary>
    public static readonly StyledProperty<IBrush> CurrentBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(CurrentBrush), Brushes.Orange);

    /// <summary>Identifies the power trace brush.</summary>
    public static readonly StyledProperty<IBrush> PowerBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(PowerBrush), Brushes.MediumPurple);

    /// <summary>Identifies the primary text brush.</summary>
    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(TextBrush), Brushes.WhiteSmoke);

    /// <summary>Identifies the secondary text brush.</summary>
    public static readonly StyledProperty<IBrush> SecondaryBrushProperty =
        AvaloniaProperty.Register<RealtimeWaveformControl, IBrush>(nameof(SecondaryBrush), Brushes.Gray);

    static RealtimeWaveformControl()
    {
        AffectsRender<RealtimeWaveformControl>(
            SnapshotProperty,
            GridBrushProperty,
            VoltageBrushProperty,
            CurrentBrushProperty,
            PowerBrushProperty,
            TextBrushProperty,
            SecondaryBrushProperty);
    }

    /// <summary>Gets or sets immutable rolling chart data.</summary>
    public WaveformChartSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    /// <summary>Gets or sets the chart-grid brush.</summary>
    public IBrush GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>Gets or sets the voltage trace brush.</summary>
    public IBrush VoltageBrush
    {
        get => GetValue(VoltageBrushProperty);
        set => SetValue(VoltageBrushProperty, value);
    }

    /// <summary>Gets or sets the current trace brush.</summary>
    public IBrush CurrentBrush
    {
        get => GetValue(CurrentBrushProperty);
        set => SetValue(CurrentBrushProperty, value);
    }

    /// <summary>Gets or sets the power trace brush.</summary>
    public IBrush PowerBrush
    {
        get => GetValue(PowerBrushProperty);
        set => SetValue(PowerBrushProperty, value);
    }

    /// <summary>Gets or sets the primary text brush.</summary>
    public IBrush TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    /// <summary>Gets or sets the secondary text brush.</summary>
    public IBrush SecondaryBrush
    {
        get => GetValue(SecondaryBrushProperty);
        set => SetValue(SecondaryBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Snapshot is null || Snapshot.Points.Count == 0 || Bounds.Width < 220.0 || Bounds.Height < 220.0)
        {
            DrawCenteredMessage(context, "Waiting for realtime samples…");
            return;
        }

        const double left = 74.0;
        var right = Bounds.Width - 16.0;
        const double top = 18.0;
        var bottom = Bounds.Height - 32.0;
        const double gap = 14.0;
        var laneHeight = (bottom - top - (2.0 * gap)) / 3.0;

        DrawLane(
            context,
            new Rect(left, top, right - left, laneHeight),
            "V",
            "V",
            Snapshot.Points.Select(point => point.Voltage).ToArray(),
            VoltageBrush);
        DrawLane(
            context,
            new Rect(left, top + laneHeight + gap, right - left, laneHeight),
            "I",
            "A",
            Snapshot.Points.Select(point => point.Current).ToArray(),
            CurrentBrush);
        DrawLane(
            context,
            new Rect(left, top + (2.0 * (laneHeight + gap)), right - left, laneHeight),
            "P",
            "W",
            Snapshot.Points.Select(point => point.Power).ToArray(),
            PowerBrush);

        DrawText(context, EngineeringFormatter.Format(Snapshot.StartTime, "s"), new Point(left, bottom + 8), SecondaryBrush, 10);
        var stopLabel = EngineeringFormatter.Format(Snapshot.StopTime, "s");
        var stopText = FormatText(stopLabel, SecondaryBrush, 10);
        context.DrawText(stopText, new Point(right - stopText.Width, bottom + 8));
    }

    private void DrawLane(
        DrawingContext context,
        Rect plot,
        string label,
        string unit,
        IReadOnlyList<double> values,
        IBrush traceBrush)
    {
        var minimum = values.Min();
        var maximum = values.Max();
        if (Math.Abs(maximum - minimum) < 1e-15)
        {
            var padding = Math.Max(1e-12, Math.Abs(maximum) * 0.1);
            minimum -= padding;
            maximum += padding;
        }
        else
        {
            var padding = (maximum - minimum) * 0.08;
            minimum -= padding;
            maximum += padding;
        }

        var gridPen = new Pen(GridBrush, 1.0);
        context.DrawRectangle(null, gridPen, plot);
        for (var division = 1; division < 4; division++)
        {
            var x = plot.Left + (plot.Width * division / 4.0);
            context.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }

        if (minimum <= 0.0 && maximum >= 0.0)
        {
            var zeroY = MapY(0.0, minimum, maximum, plot);
            context.DrawLine(gridPen, new Point(plot.Left, zeroY), new Point(plot.Right, zeroY));
        }

        DrawText(context, label, new Point(10, plot.Top + 5), traceBrush, 15);
        DrawText(context, EngineeringFormatter.Format(maximum, unit), new Point(10, plot.Top + 25), SecondaryBrush, 9);
        DrawText(context, EngineeringFormatter.Format(minimum, unit), new Point(10, plot.Bottom - 14), SecondaryBrush, 9);

        var tracePen = new Pen(traceBrush, 1.7);
        Point? previous = null;
        for (var index = 0; index < Snapshot!.Points.Count; index++)
        {
            var point = Snapshot.Points[index];
            var x = plot.Left + ((point.Time - Snapshot.StartTime) /
                (Snapshot.StopTime - Snapshot.StartTime) * plot.Width);
            var current = new Point(x, MapY(values[index], minimum, maximum, plot));
            if (previous is { } prior)
            {
                context.DrawLine(tracePen, prior, current);
            }

            previous = current;
        }
    }

    private static double MapY(double value, double minimum, double maximum, Rect plot) =>
        plot.Bottom - ((value - minimum) / (maximum - minimum) * plot.Height);

    private void DrawCenteredMessage(DrawingContext context, string message)
    {
        var formatted = FormatText(message, SecondaryBrush, 13);
        context.DrawText(formatted, new Point(
            (Bounds.Width - formatted.Width) / 2.0,
            (Bounds.Height - formatted.Height) / 2.0));
    }

    private static void DrawText(
        DrawingContext context,
        string text,
        Point origin,
        IBrush brush,
        double size) => context.DrawText(FormatText(text, brush, size), origin);

    private static FormattedText FormatText(string text, IBrush brush, double size) =>
        new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter"),
            size,
            brush);
}
