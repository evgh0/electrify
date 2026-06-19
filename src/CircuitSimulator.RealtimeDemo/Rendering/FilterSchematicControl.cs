using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;
using CircuitSimulator.RealtimeDemo.Models;

namespace CircuitSimulator.RealtimeDemo.Rendering;

/// <summary>Draws and hit-tests the fixed two-stage RC low-pass schematic.</summary>
public sealed class FilterSchematicControl : Control
{
    private const double WorldWidth = 940.0;
    private const double WorldHeight = 560.0;

    /// <summary>Identifies the predefined circuit.</summary>
    public static readonly StyledProperty<RealtimeDemoCircuit?> CircuitProperty =
        AvaloniaProperty.Register<FilterSchematicControl, RealtimeDemoCircuit?>(nameof(Circuit));

    /// <summary>Identifies the selected component.</summary>
    public static readonly StyledProperty<ComponentId?> SelectedComponentIdProperty =
        AvaloniaProperty.Register<FilterSchematicControl, ComponentId?>(nameof(SelectedComponentId));

    /// <summary>Identifies the wire brush.</summary>
    public static readonly StyledProperty<IBrush> WireBrushProperty =
        AvaloniaProperty.Register<FilterSchematicControl, IBrush>(nameof(WireBrush), Brushes.LightGray);

    /// <summary>Identifies the symbol brush.</summary>
    public static readonly StyledProperty<IBrush> SymbolBrushProperty =
        AvaloniaProperty.Register<FilterSchematicControl, IBrush>(nameof(SymbolBrush), Brushes.WhiteSmoke);

    /// <summary>Identifies the secondary text brush.</summary>
    public static readonly StyledProperty<IBrush> SecondaryBrushProperty =
        AvaloniaProperty.Register<FilterSchematicControl, IBrush>(nameof(SecondaryBrush), Brushes.Gray);

    /// <summary>Identifies the selection brush.</summary>
    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<FilterSchematicControl, IBrush>(nameof(AccentBrush), Brushes.Aquamarine);

    private WorldTransform _transform = new(1.0, 0.0, 0.0);

    static FilterSchematicControl()
    {
        AffectsRender<FilterSchematicControl>(
            CircuitProperty,
            SelectedComponentIdProperty,
            WireBrushProperty,
            SymbolBrushProperty,
            SecondaryBrushProperty,
            AccentBrushProperty);
        FocusableProperty.OverrideDefaultValue<FilterSchematicControl>(true);
    }

    /// <summary>Raised when a schematic symbol is selected.</summary>
    public event EventHandler<ComponentId>? ComponentSelected;

    /// <summary>Gets or sets the predefined circuit.</summary>
    public RealtimeDemoCircuit? Circuit
    {
        get => GetValue(CircuitProperty);
        set => SetValue(CircuitProperty, value);
    }

    /// <summary>Gets or sets the highlighted component identifier.</summary>
    public ComponentId? SelectedComponentId
    {
        get => GetValue(SelectedComponentIdProperty);
        set => SetValue(SelectedComponentIdProperty, value);
    }

    /// <summary>Gets or sets the wire brush.</summary>
    public IBrush WireBrush
    {
        get => GetValue(WireBrushProperty);
        set => SetValue(WireBrushProperty, value);
    }

    /// <summary>Gets or sets the component-symbol brush.</summary>
    public IBrush SymbolBrush
    {
        get => GetValue(SymbolBrushProperty);
        set => SetValue(SymbolBrushProperty, value);
    }

    /// <summary>Gets or sets the secondary text brush.</summary>
    public IBrush SecondaryBrush
    {
        get => GetValue(SecondaryBrushProperty);
        set => SetValue(SecondaryBrushProperty, value);
    }

    /// <summary>Gets or sets the selection accent brush.</summary>
    public IBrush AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Circuit is null || Bounds.Width <= 0.0 || Bounds.Height <= 0.0)
        {
            return;
        }

        _transform = CalculateTransform(Bounds.Size);
        var wirePen = new Pen(WireBrush, 2.0);
        DrawWire(context, wirePen, (120, 250), (120, 150), (280, 150));
        DrawWire(context, wirePen, (380, 150), (480, 150), (480, 250));
        DrawWire(context, wirePen, (480, 150), (600, 150));
        DrawWire(context, wirePen, (700, 150), (820, 150), (820, 250));
        DrawWire(context, wirePen, (120, 350), (120, 450), (820, 450));
        DrawWire(context, wirePen, (480, 350), (480, 450));
        DrawWire(context, wirePen, (820, 350), (820, 450));
        DrawGround(context, ToScreen(new Point(480, 450)));
        DrawNode(context, new Point(480, 150));
        DrawNode(context, new Point(480, 450));

        foreach (var placement in GetPlacements(Circuit))
        {
            DrawComponent(context, placement);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Circuit is null)
        {
            return;
        }

        Focus();
        var point = ToWorld(e.GetPosition(this));
        var hit = GetPlacements(Circuit)
            .Where(placement => Math.Abs(point.X - placement.Center.X) <= 62.0 &&
                Math.Abs(point.Y - placement.Center.Y) <= 62.0)
            .OrderBy(placement => DistanceSquared(point, placement.Center))
            .FirstOrDefault();
        if (hit is null)
        {
            return;
        }

        SelectedComponentId = hit.ComponentId;
        ComponentSelected?.Invoke(this, hit.ComponentId);
        e.Handled = true;
    }

    private void DrawComponent(DrawingContext context, Placement placement)
    {
        var center = ToScreen(placement.Center);
        var selected = SelectedComponentId == placement.ComponentId;
        if (selected)
        {
            var selection = new Rect(center.X - 45.0, center.Y - 44.0, 90.0, 88.0);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(28, 97, 212, 179)), new Pen(AccentBrush, 1.5), selection, 7, 7);
        }

        var pen = new Pen(selected ? AccentBrush : SymbolBrush, selected ? 2.5 : 1.8);
        Point Local(double primary, double secondary) => placement.Vertical
            ? new Point(center.X + secondary, center.Y + primary)
            : new Point(center.X + primary, center.Y + secondary);
        var terminalPen = new Pen(WireBrush, 1.8);
        context.DrawLine(terminalPen, Local(-50, 0), Local(-28, 0));
        context.DrawLine(terminalPen, Local(28, 0), Local(50, 0));

        switch (placement.Kind)
        {
            case ComponentKind.VoltageSource:
                DrawSource(context, pen, Local);
                break;
            case ComponentKind.Resistor:
                DrawResistor(context, pen, Local);
                break;
            case ComponentKind.Capacitor:
                context.DrawLine(pen, Local(-7, -18), Local(-7, 18));
                context.DrawLine(pen, Local(7, -18), Local(7, 18));
                break;
        }

        var labelOrigin = placement.Vertical
            ? new Point(center.X + 37, center.Y - 19)
            : new Point(center.X - 15, center.Y + 42);
        DrawText(context, placement.Name, labelOrigin, selected ? AccentBrush : SymbolBrush, 13);
        DrawText(context, placement.Value, new Point(labelOrigin.X, labelOrigin.Y + 17), SecondaryBrush, 10);
    }

    private static void DrawResistor(DrawingContext context, Pen pen, Func<double, double, Point> local)
    {
        var points = new[]
        {
            local(-28, 0), local(-22, -9), local(-14, 9), local(-6, -9),
            local(2, 9), local(10, -9), local(18, 9), local(28, 0)
        };
        for (var index = 1; index < points.Length; index++)
        {
            context.DrawLine(pen, points[index - 1], points[index]);
        }
    }

    private static void DrawSource(DrawingContext context, Pen pen, Func<double, double, Point> local)
    {
        var center = local(0, 0);
        context.DrawEllipse(null, pen, center, 27, 27);
        var previous = local(-15, 0);
        for (var index = 1; index <= 24; index++)
        {
            var primary = -15.0 + (30.0 * index / 24.0);
            var secondary = Math.Sin(index * Math.PI / 6.0) * 6.0;
            var next = local(primary, secondary);
            context.DrawLine(pen, previous, next);
            previous = next;
        }
    }

    private void DrawGround(DrawingContext context, Point point)
    {
        var pen = new Pen(WireBrush, 1.6);
        context.DrawLine(pen, point, new Point(point.X, point.Y + 8));
        context.DrawLine(pen, new Point(point.X - 15, point.Y + 8), new Point(point.X + 15, point.Y + 8));
        context.DrawLine(pen, new Point(point.X - 10, point.Y + 14), new Point(point.X + 10, point.Y + 14));
        context.DrawLine(pen, new Point(point.X - 5, point.Y + 20), new Point(point.X + 5, point.Y + 20));
    }

    private void DrawNode(DrawingContext context, Point worldPoint) =>
        context.DrawEllipse(WireBrush, null, ToScreen(worldPoint), 3.5, 3.5);

    private void DrawWire(DrawingContext context, Pen pen, params (double X, double Y)[] points)
    {
        for (var index = 1; index < points.Length; index++)
        {
            context.DrawLine(
                pen,
                ToScreen(new Point(points[index - 1].X, points[index - 1].Y)),
                ToScreen(new Point(points[index].X, points[index].Y)));
        }
    }

    private static IReadOnlyList<Placement> GetPlacements(RealtimeDemoCircuit circuit) =>
    [
        new(circuit.SourceId, "V1", "5 Vpk · 1 Hz", ComponentKind.VoltageSource, new Point(120, 300), true),
        new(circuit.FirstResistorId, "R1", "1 kΩ", ComponentKind.Resistor, new Point(330, 150), false),
        new(circuit.FirstCapacitorId, "C1", "100 µF", ComponentKind.Capacitor, new Point(480, 300), true),
        new(circuit.SecondResistorId, "R2", "1 kΩ", ComponentKind.Resistor, new Point(650, 150), false),
        new(circuit.SecondCapacitorId, "C2", "220 µF", ComponentKind.Capacitor, new Point(820, 300), true)
    ];

    private Point ToScreen(Point point) => new(
        (point.X * _transform.Scale) + _transform.OffsetX,
        (point.Y * _transform.Scale) + _transform.OffsetY);

    private Point ToWorld(Point point) => new(
        (point.X - _transform.OffsetX) / _transform.Scale,
        (point.Y - _transform.OffsetY) / _transform.Scale);

    private static WorldTransform CalculateTransform(Size size)
    {
        const double margin = 24.0;
        var scale = Math.Min(
            Math.Max(1.0, size.Width - (2.0 * margin)) / WorldWidth,
            Math.Max(1.0, size.Height - (2.0 * margin)) / WorldHeight);
        scale = Math.Clamp(scale, 0.3, 1.25);
        return new WorldTransform(
            scale,
            (size.Width - (WorldWidth * scale)) / 2.0,
            (size.Height - (WorldHeight * scale)) / 2.0);
    }

    private static double DistanceSquared(Point first, Point second) =>
        Math.Pow(first.X - second.X, 2.0) + Math.Pow(first.Y - second.Y, 2.0);

    private static void DrawText(
        DrawingContext context,
        string text,
        Point origin,
        IBrush brush,
        double size)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter"),
            size,
            brush);
        context.DrawText(formatted, origin);
    }

    private sealed record Placement(
        ComponentId ComponentId,
        string Name,
        string Value,
        ComponentKind Kind,
        Point Center,
        bool Vertical);

    private readonly record struct WorldTransform(double Scale, double OffsetX, double OffsetY);
}
