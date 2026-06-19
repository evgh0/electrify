using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CircuitSimulator.Editor.Charts;
using CircuitSimulator.Editor.Properties;

namespace CircuitSimulator.Editor.Rendering;

/// <summary>Custom-drawn synchronized transient voltage/current/power chart.</summary>
public sealed class TransientChartControl : Control
{
    /// <summary>Identifies chart data.</summary>
    public static readonly StyledProperty<TransientChartData?> DataProperty =
        AvaloniaProperty.Register<TransientChartControl, TransientChartData?>(nameof(Data));

    /// <summary>Identifies the grid brush.</summary>
    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<TransientChartControl, IBrush>(nameof(GridBrush), Brushes.DimGray);

    /// <summary>Identifies the primary trace brush.</summary>
    public static readonly StyledProperty<IBrush> TraceBrushProperty =
        AvaloniaProperty.Register<TransientChartControl, IBrush>(nameof(TraceBrush), Brushes.MediumPurple);

    /// <summary>Identifies the reference trace brush.</summary>
    public static readonly StyledProperty<IBrush> ReferenceBrushProperty =
        AvaloniaProperty.Register<TransientChartControl, IBrush>(nameof(ReferenceBrush), Brushes.CornflowerBlue);

    /// <summary>Identifies text brush.</summary>
    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<TransientChartControl, IBrush>(nameof(TextBrush), Brushes.LightGray);

    /// <summary>Identifies secondary text brush.</summary>
    public static readonly StyledProperty<IBrush> SecondaryBrushProperty =
        AvaloniaProperty.Register<TransientChartControl, IBrush>(nameof(SecondaryBrush), Brushes.Gray);

    private double? _cursorTime;

    static TransientChartControl()
    {
        AffectsRender<TransientChartControl>(
            DataProperty,
            GridBrushProperty,
            TraceBrushProperty,
            ReferenceBrushProperty,
            TextBrushProperty,
            SecondaryBrushProperty);
        DataProperty.Changed.AddClassHandler<TransientChartControl>((control, _) => control.OnDataChanged());
        FocusableProperty.OverrideDefaultValue<TransientChartControl>(true);
    }

    /// <summary>Raised when pointer or keyboard navigation changes the shared cursor.</summary>
    public event EventHandler<double>? CursorTimeChanged;

    /// <summary>Gets or sets prepared chart data.</summary>
    public TransientChartData? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets grid brush.</summary>
    public IBrush GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>Gets or sets primary trace brush.</summary>
    public IBrush TraceBrush
    {
        get => GetValue(TraceBrushProperty);
        set => SetValue(TraceBrushProperty, value);
    }

    /// <summary>Gets or sets reference trace brush.</summary>
    public IBrush ReferenceBrush
    {
        get => GetValue(ReferenceBrushProperty);
        set => SetValue(ReferenceBrushProperty, value);
    }

    /// <summary>Gets or sets main text brush.</summary>
    public IBrush TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    /// <summary>Gets or sets secondary text brush.</summary>
    public IBrush SecondaryBrush
    {
        get => GetValue(SecondaryBrushProperty);
        set => SetValue(SecondaryBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Data is null || Bounds.Width < 160.0 || Bounds.Height < 150.0)
        {
            DrawCenteredMessage(context, "Run a simulation to inspect waveforms");
            return;
        }

        var plotLeft = 70.0;
        var plotRight = Bounds.Width - 18.0;
        var plotTop = 25.0;
        var plotBottom = Bounds.Height - 30.0;
        var laneGap = 12.0;
        var laneHeight = (plotBottom - plotTop - (laneGap * 2.0)) / 3.0;
        for (var index = 0; index < Data.Lanes.Count; index++)
        {
            var top = plotTop + (index * (laneHeight + laneGap));
            DrawLane(context, Data.Lanes[index], new Rect(plotLeft, top, plotRight - plotLeft, laneHeight));
        }

        DrawText(context, EngineeringNotation.Format(Data.StartTime, "s"), new Point(plotLeft, plotBottom + 8), SecondaryBrush, 11);
        var stopLabel = EngineeringNotation.Format(Data.StopTime, "s");
        DrawText(context, stopLabel, new Point(plotRight - 42, plotBottom + 8), SecondaryBrush, 11);

        if (_cursorTime is { } cursor)
        {
            var x = MapX(cursor, plotLeft, plotRight);
            context.DrawLine(new Pen(TextBrush, 1.0), new Point(x, plotTop), new Point(x, plotBottom));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (Data is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        var left = 70.0;
        var right = Bounds.Width - 18.0;
        if (point.X < left || point.X > right)
        {
            return;
        }

        SetCursor(Data.StartTime + ((point.X - left) / (right - left) * (Data.StopTime - Data.StartTime)));
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Data is null || e.Key is not (Key.Left or Key.Right))
        {
            return;
        }

        var direction = e.Key == Key.Left ? -1.0 : 1.0;
        var current = _cursorTime ?? Data.StopTime;
        SetCursor(current + (direction * (Data.StopTime - Data.StartTime) / 500.0));
        e.Handled = true;
    }

    private void DrawLane(DrawingContext context, ChartLane lane, Rect plot)
    {
        var gridPen = new Pen(GridBrush, 1.0);
        context.DrawRectangle(null, gridPen, plot);
        for (var division = 1; division < 4; division++)
        {
            var x = plot.Left + (plot.Width * division / 4.0);
            context.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }

        var zero = MapY(0.0, lane, plot);
        if (zero >= plot.Top && zero <= plot.Bottom)
        {
            context.DrawLine(gridPen, new Point(plot.Left, zero), new Point(plot.Right, zero));
        }

        DrawText(context, lane.Kind.ToString(), new Point(8, plot.Top + 4), TextBrush, 12);
        DrawText(context, EngineeringNotation.Format(lane.Maximum, lane.Unit), new Point(8, plot.Top + 22), SecondaryBrush, 10);
        DrawText(context, EngineeringNotation.Format(lane.Minimum, lane.Unit), new Point(8, plot.Bottom - 15), SecondaryBrush, 10);

        foreach (var series in lane.Series)
        {
            var brush = series.IsReference ? ReferenceBrush : TraceBrush;
            var pen = series.IsReference
                ? new Pen(brush, 1.3, new DashStyle([5.0, 4.0], 0.0))
                : new Pen(brush, 1.8);
            var points = TransientChartData.Decimate(series.Points, Math.Max(1, (int)plot.Width));
            Point? previous = null;
            foreach (var point in points)
            {
                var current = new Point(MapX(point.Time, plot.Left, plot.Right), MapY(point.Value, lane, plot));
                if (previous is { } prior)
                {
                    context.DrawLine(pen, prior, current);
                }

                previous = current;
            }
        }
    }

    private double MapX(double time, double left, double right)
    {
        if (Data is null || Data.StopTime <= Data.StartTime)
        {
            return left;
        }

        return left + ((time - Data.StartTime) / (Data.StopTime - Data.StartTime) * (right - left));
    }

    private static double MapY(double value, ChartLane lane, Rect plot) =>
        plot.Bottom - ((value - lane.Minimum) / (lane.Maximum - lane.Minimum) * plot.Height);

    private void SetCursor(double time)
    {
        if (Data is null)
        {
            return;
        }

        var sample = Data.FindNearest(Math.Clamp(time, Data.StartTime, Data.StopTime));
        _cursorTime = sample.Time;
        CursorTimeChanged?.Invoke(this, sample.Time);
        InvalidateVisual();
    }

    private void OnDataChanged()
    {
        _cursorTime = Data?.StopTime;
        InvalidateVisual();
    }

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
