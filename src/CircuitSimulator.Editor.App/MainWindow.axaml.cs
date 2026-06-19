using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CircuitSimulator.Editor.Rendering;
using CircuitSimulator.Editor.ViewModels;

namespace CircuitSimulator.Editor.App;

/// <summary>Main desktop workbench window.</summary>
public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    /// <summary>Initializes the workbench.</summary>
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        var diagram = this.FindControl<CircuitDiagramControl>("CircuitDiagram")!;
        var chart = this.FindControl<TransientChartControl>("WaveformChart")!;
        diagram.ComponentSelected += (_, key) => _viewModel.SelectComponent(key);
        chart.CursorTimeChanged += (_, time) => _viewModel.SetCursorTime(time);
        Opened += OnOpened;
        Closed += (_, _) => _viewModel.Dispose();
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await _viewModel.StartAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
