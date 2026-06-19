using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Editor.Contracts.Documents;
using CircuitSimulator.Editor.Contracts.Geometry;
using CircuitSimulator.Editor.Documents;

namespace CircuitSimulator.Editor.Rendering;

/// <summary>Custom-drawn, selectable fixed demo schematic.</summary>
public sealed class CircuitDiagramControl : Control
{
    /// <summary>Identifies the demo instance property.</summary>
    public static readonly StyledProperty<DemoCircuitInstance?> CircuitProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, DemoCircuitInstance?>(nameof(Circuit));

    /// <summary>Identifies the selected component property.</summary>
    public static readonly StyledProperty<DemoComponentKey?> SelectedComponentProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, DemoComponentKey?>(nameof(SelectedComponent));

    /// <summary>Identifies the wire brush property.</summary>
    public static readonly StyledProperty<IBrush> WireBrushProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, IBrush>(nameof(WireBrush), Brushes.LightGray);

    /// <summary>Identifies the symbol brush property.</summary>
    public static readonly StyledProperty<IBrush> SymbolBrushProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, IBrush>(nameof(SymbolBrush), Brushes.White);

    /// <summary>Identifies the secondary text brush property.</summary>
    public static readonly StyledProperty<IBrush> SecondaryBrushProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, IBrush>(nameof(SecondaryBrush), Brushes.Gray);

    /// <summary>Identifies the accent brush property.</summary>
    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, IBrush>(nameof(AccentBrush), Brushes.MediumPurple);

    /// <summary>Identifies the selection fill brush property.</summary>
    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<CircuitDiagramControl, IBrush>(nameof(SelectionBrush), Brushes.Transparent);

    private WorldTransform _transform;

    static CircuitDiagramControl()
    {
        AffectsRender<CircuitDiagramControl>(
            CircuitProperty,
            SelectedComponentProperty,
            WireBrushProperty,
            SymbolBrushProperty,
            SecondaryBrushProperty,
            AccentBrushProperty,
            SelectionBrushProperty);
        FocusableProperty.OverrideDefaultValue<CircuitDiagramControl>(true);
    }

    /// <summary>Raised when the user selects a component.</summary>
    public event EventHandler<DemoComponentKey>? ComponentSelected;

    /// <summary>Gets or sets the built demo instance.</summary>
    public DemoCircuitInstance? Circuit
    {
        get => GetValue(CircuitProperty);
        set => SetValue(CircuitProperty, value);
    }

    /// <summary>Gets or sets the selected component.</summary>
    public DemoComponentKey? SelectedComponent
    {
        get => GetValue(SelectedComponentProperty);
        set => SetValue(SelectedComponentProperty, value);
    }

    /// <summary>Gets or sets the wire brush.</summary>
    public IBrush WireBrush
    {
        get => GetValue(WireBrushProperty);
        set => SetValue(WireBrushProperty, value);
    }

    /// <summary>Gets or sets the symbol brush.</summary>
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

    /// <summary>Gets or sets the selection fill brush.</summary>
    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Circuit is null || Bounds.Width <= 0.0 || Bounds.Height <= 0.0)
        {
            return;
        }

        _transform = CalculateTransform(Circuit.Schematic, Bounds.Size);
        var wirePen = new Pen(WireBrush, 2.0);
        foreach (var wire in Circuit.Schematic.Wires)
        {
            for (var index = 1; index < wire.Points.Count; index++)
            {
                context.DrawLine(wirePen, ToScreen(wire.Points[index - 1]), ToScreen(wire.Points[index]));
            }
        }

        DrawGround(context, Circuit.Schematic.GroundPosition);
        foreach (var placement in Circuit.Schematic.Components)
        {
            DrawComponent(context, placement);
        }
    }

    /// <summary>Hit-tests a screen point using a zoom-aware symbol body tolerance.</summary>
    public DemoComponentKey? HitTestComponent(Point point)
    {
        if (Circuit is null)
        {
            return null;
        }

        var world = new Point2(
            (point.X - _transform.OffsetX) / _transform.Scale,
            (point.Y - _transform.OffsetY) / _transform.Scale);
        return SchematicHitTest.FindNearest(
            Circuit.Schematic,
            world,
            Math.Max(58.0, 32.0 / _transform.Scale));
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var hit = HitTestComponent(e.GetPosition(this));
        if (hit is { } key)
        {
            SelectedComponent = key;
            ComponentSelected?.Invoke(this, key);
            e.Handled = true;
        }
    }

    private void DrawComponent(DrawingContext context, SchematicComponentPlacement placement)
    {
        if (Circuit is null)
        {
            return;
        }

        var component = Circuit.Circuit.GetComponent(placement.ComponentId);
        var center = ToScreen(placement.Position);
        var selected = SelectedComponent == placement.Key;
        if (selected)
        {
            var selectionRect = new Rect(center.X - 46, center.Y - 42, 92, 84);
            context.DrawRectangle(SelectionBrush, new Pen(AccentBrush, 1.5), selectionRect, 8, 8);
        }

        var pen = new Pen(selected ? AccentBrush : SymbolBrush, selected ? 2.4 : 1.8);
        var terminalPen = new Pen(WireBrush, 1.8);
        var horizontal = placement.Orientation == ComponentOrientation.Horizontal;
        Point Local(double primary, double secondary) => horizontal
            ? new Point(center.X + primary, center.Y + secondary)
            : new Point(center.X + secondary, center.Y + primary);

        context.DrawLine(terminalPen, Local(-50, 0), Local(-28, 0));
        context.DrawLine(terminalPen, Local(28, 0), Local(50, 0));
        context.DrawEllipse(SymbolBrush, null, Local(-50, 0), 2.5, 2.5);
        context.DrawEllipse(SymbolBrush, null, Local(50, 0), 2.5, 2.5);

        switch (component.Kind)
        {
            case ComponentKind.Resistor:
                DrawResistor(context, pen, Local);
                break;
            case ComponentKind.Capacitor:
                context.DrawLine(pen, Local(-7, -18), Local(-7, 18));
                context.DrawLine(pen, Local(7, -18), Local(7, 18));
                break;
            case ComponentKind.Inductor:
                DrawInductor(context, pen, Local);
                break;
            case ComponentKind.Diode:
                DrawDiode(context, pen, Local);
                break;
            case ComponentKind.VoltageSource:
            case ComponentKind.CurrentSource:
                DrawSource(context, pen, Local, component.Kind == ComponentKind.VoltageSource);
                break;
        }

        DrawLabel(context, component.Name, new Point(center.X - 14, center.Y + 48), SymbolBrush, 13);
    }

    private static void DrawResistor(DrawingContext context, Pen pen, Func<double, double, Point> local)
    {
        var points = new[]
        {
            local(-28, 0), local(-22, -10), local(-14, 10), local(-6, -10),
            local(2, 10), local(10, -10), local(18, 10), local(28, 0)
        };
        for (var index = 1; index < points.Length; index++)
        {
            context.DrawLine(pen, points[index - 1], points[index]);
        }
    }

    private static void DrawInductor(DrawingContext context, Pen pen, Func<double, double, Point> local)
    {
        var previous = local(-28, 0);
        const int segments = 32;
        for (var index = 1; index <= segments; index++)
        {
            var x = -28.0 + (56.0 * index / segments);
            var y = -Math.Abs(Math.Sin(index * Math.PI / 8.0)) * 12.0;
            var next = local(x, y);
            context.DrawLine(pen, previous, next);
            previous = next;
        }
    }

    private static void DrawDiode(DrawingContext context, Pen pen, Func<double, double, Point> local)
    {
        context.DrawLine(pen, local(-18, -17), local(-18, 17));
        context.DrawLine(pen, local(-18, -17), local(13, 0));
        context.DrawLine(pen, local(-18, 17), local(13, 0));
        context.DrawLine(pen, local(15, -18), local(15, 18));
    }

    private static void DrawSource(
        DrawingContext context,
        Pen pen,
        Func<double, double, Point> local,
        bool voltageSource)
    {
        var center = local(0, 0);
        context.DrawEllipse(null, pen, center, 27, 27);
        if (voltageSource)
        {
            context.DrawLine(pen, local(-10, -6), local(-10, 6));
            context.DrawLine(pen, local(-16, 0), local(-4, 0));
            context.DrawLine(pen, local(6, 0), local(16, 0));
        }
        else
        {
            context.DrawLine(pen, local(12, 0), local(-10, 0));
            context.DrawLine(pen, local(-10, 0), local(-2, -7));
            context.DrawLine(pen, local(-10, 0), local(-2, 7));
        }
    }

    private void DrawGround(DrawingContext context, Point2 position)
    {
        var point = ToScreen(position);
        var pen = new Pen(WireBrush, 1.6);
        context.DrawLine(pen, point, new Point(point.X, point.Y + 7));
        context.DrawLine(pen, new Point(point.X - 14, point.Y + 7), new Point(point.X + 14, point.Y + 7));
        context.DrawLine(pen, new Point(point.X - 9, point.Y + 12), new Point(point.X + 9, point.Y + 12));
        context.DrawLine(pen, new Point(point.X - 4, point.Y + 17), new Point(point.X + 4, point.Y + 17));
    }

    private static void DrawLabel(
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

    private Point ToScreen(Point2 point) => new(
        (point.X * _transform.Scale) + _transform.OffsetX,
        (point.Y * _transform.Scale) + _transform.OffsetY);

    private static WorldTransform CalculateTransform(DemoSchematic schematic, Size size)
    {
        var allPoints = schematic.Wires.SelectMany(wire => wire.Points)
            .Concat(schematic.Components.Select(component => component.Position))
            .Append(schematic.GroundPosition)
            .ToArray();
        var minX = allPoints.Min(point => point.X) - 65.0;
        var maxX = allPoints.Max(point => point.X) + 65.0;
        var minY = allPoints.Min(point => point.Y) - 65.0;
        var maxY = allPoints.Max(point => point.Y) + 65.0;
        var scale = Math.Min(size.Width / (maxX - minX), size.Height / (maxY - minY));
        scale = Math.Clamp(scale, 0.35, 1.4);
        var offsetX = ((size.Width - ((maxX - minX) * scale)) / 2.0) - (minX * scale);
        var offsetY = ((size.Height - ((maxY - minY) * scale)) / 2.0) - (minY * scale);
        return new WorldTransform(scale, offsetX, offsetY);
    }

    private readonly record struct WorldTransform(double Scale, double OffsetX, double OffsetY);
}
