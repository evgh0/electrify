using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using CircuitSimulator.Core.Model;
using CircuitSimulator.RealtimeDemo.Formatting;
using CircuitSimulator.RealtimeDemo.Models;
using CircuitSimulator.RealtimeDemo.Rendering;
using CircuitSimulator.RealtimeDemo.Simulation;

namespace CircuitSimulator.RealtimeDemo;

/// <summary>Main window for the standalone realtime circuit demonstration.</summary>
public sealed partial class MainWindow : Window
{
    private static readonly IBrush RunningBrush = new SolidColorBrush(Color.Parse("#61D4B3"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6B7A"));

    private readonly RealtimeDemoCircuit _circuit;
    private readonly RealtimeDemoController _controller;
    private readonly DispatcherTimer _refreshTimer;
    private readonly RealtimeWaveformControl _chart;
    private readonly FilterSchematicControl _diagram;
    private readonly ComboBox _selector;
    private readonly TextBlock _chartComponentText;
    private readonly TextBlock _timeValue;
    private readonly TextBlock _voltageValue;
    private readonly TextBlock _currentValue;
    private readonly TextBlock _powerValue;
    private readonly TextBlock _statusIndicator;
    private readonly TextBlock _statusText;
    private ComponentId _selectedComponentId;

    /// <summary>Initializes the fixed realtime demonstration window.</summary>
    public MainWindow()
    {
        InitializeComponent();
        _circuit = RealtimeDemoCircuit.Create();
        _controller = RealtimeDemoController.Create(_circuit);
        _selectedComponentId = _circuit.DefaultSelectedComponentId;
        _chart = this.FindControl<RealtimeWaveformControl>("WaveformChart")!;
        _diagram = this.FindControl<FilterSchematicControl>("CircuitDiagram")!;
        _selector = this.FindControl<ComboBox>("ComponentSelector")!;
        _chartComponentText = this.FindControl<TextBlock>("ChartComponentText")!;
        _timeValue = this.FindControl<TextBlock>("TimeValue")!;
        _voltageValue = this.FindControl<TextBlock>("VoltageValue")!;
        _currentValue = this.FindControl<TextBlock>("CurrentValue")!;
        _powerValue = this.FindControl<TextBlock>("PowerValue")!;
        _statusIndicator = this.FindControl<TextBlock>("StatusIndicator")!;
        _statusText = this.FindControl<TextBlock>("StatusText")!;

        _selector.ItemsSource = _circuit.Components;
        _selector.SelectedItem = _circuit.GetComponent(_selectedComponentId);
        _selector.SelectionChanged += OnSelectionChanged;
        _diagram.Circuit = _circuit;
        _diagram.SelectedComponentId = _selectedComponentId;
        _diagram.ComponentSelected += OnDiagramComponentSelected;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33.0) };
        _refreshTimer.Tick += (_, _) => RefreshUi();
        Opened += OnOpened;
        Closed += OnClosed;
        RefreshUi();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        _controller.Start();
        _refreshTimer.Start();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        await _controller.DisposeAsync();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_selector.SelectedItem is DemoComponentDescriptor descriptor)
        {
            SelectComponent(descriptor.ComponentId, updateSelector: false);
        }
    }

    private void OnDiagramComponentSelected(object? sender, ComponentId componentId) =>
        SelectComponent(componentId, updateSelector: true);

    private void SelectComponent(ComponentId componentId, bool updateSelector)
    {
        _selectedComponentId = componentId;
        _diagram.SelectedComponentId = componentId;
        if (updateSelector)
        {
            _selector.SelectedItem = _circuit.GetComponent(componentId);
        }

        RefreshUi();
    }

    private void RefreshUi()
    {
        var snapshot = _controller.CreateSnapshot(_selectedComponentId);
        _chart.Snapshot = snapshot;
        _chartComponentText.Text = $"{snapshot.Component.Name} · {snapshot.Component.Kind}";
        if (snapshot.Latest is { } latest)
        {
            _timeValue.Text = EngineeringFormatter.Format(latest.Time, "s");
            _voltageValue.Text = EngineeringFormatter.Format(latest.Voltage, "V");
            _currentValue.Text = EngineeringFormatter.Format(latest.Current, "A");
            _powerValue.Text = EngineeringFormatter.Format(latest.Power, "W");
        }

        switch (_controller.State)
        {
            case RealtimeDemoState.NotStarted:
                SetStatus("Ready", Brushes.Gray);
                break;
            case RealtimeDemoState.Running:
                SetStatus("Realtime simulation running", RunningBrush);
                break;
            case RealtimeDemoState.Stopped:
                SetStatus("Realtime simulation stopped", Brushes.Gray);
                break;
            case RealtimeDemoState.Failed:
                SetStatus($"Simulation failed: {_controller.ErrorMessage}", ErrorBrush);
                break;
        }
    }

    private void SetStatus(string text, IBrush indicatorBrush)
    {
        _statusText.Text = text;
        _statusIndicator.Foreground = indicatorBrush;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
