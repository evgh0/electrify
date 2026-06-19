using System.Collections.ObjectModel;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Editor.Charts;
using CircuitSimulator.Editor.Contracts.Documents;
using CircuitSimulator.Editor.Documents;
using CircuitSimulator.Editor.Properties;
using CircuitSimulator.Editor.Simulation;

namespace CircuitSimulator.Editor.ViewModels;

/// <summary>Coordinates the focused transient simulation workbench.</summary>
public sealed class MainWindowViewModel : BindableBase, IDisposable
{
    private readonly SimulationSession _session;
    private readonly Dictionary<string, DemoParameterValues> _valuesByDefinition = [];
    private DemoCircuitDefinition _selectedDefinition;
    private DemoCircuitInstance _currentInstance;
    private DemoComponentKey _selectedComponent;
    private ComponentListItemViewModel? _selectedComponentItem;
    private TransientChartData? _chartData;
    private ChartCursorSample? _cursor;

    /// <summary>Initializes the transient showcase view model.</summary>
    public MainWindowViewModel(SimulationSession? session = null)
    {
        _session = session ?? new SimulationSession();
        _session.Changed += OnSessionChanged;
        Examples = DemoCatalog.All;
        _selectedDefinition = Examples[0];
        _valuesByDefinition[_selectedDefinition.Id] = _selectedDefinition.Defaults;
        _currentInstance = _selectedDefinition.Build(_selectedDefinition.Defaults);
        _selectedComponent = _selectedDefinition.DefaultSelectedComponent;
        ApplyAndRunCommand = new AsyncRelayCommand(ApplyAndRunAsync, () => !IsRunning);
        RunCommand = new AsyncRelayCommand(RunAsync, () => !IsRunning);
        ResetCommand = new RelayCommand(Reset, () => !IsRunning);
        CancelCommand = new RelayCommand(_session.Cancel, () => IsRunning);
        RebuildFieldsAndComponents();
    }

    /// <summary>Gets available examples.</summary>
    public IReadOnlyList<DemoCircuitDefinition> Examples { get; }

    /// <summary>Gets parameter fields visible for the selected component and simulation.</summary>
    public ObservableCollection<ParameterFieldViewModel> ParameterFields { get; } = [];

    /// <summary>Gets selectable components in circuit order.</summary>
    public ObservableCollection<ComponentListItemViewModel> Components { get; } = [];

    /// <summary>Gets the apply-and-run command.</summary>
    public AsyncRelayCommand ApplyAndRunCommand { get; }

    /// <summary>Gets the rerun command.</summary>
    public AsyncRelayCommand RunCommand { get; }

    /// <summary>Gets the reset command.</summary>
    public RelayCommand ResetCommand { get; }

    /// <summary>Gets the cancellation command.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the selected example.</summary>
    public DemoCircuitDefinition SelectedDefinition
    {
        get => _selectedDefinition;
        set
        {
            if (value is null || !SetProperty(ref _selectedDefinition, value))
            {
                return;
            }

            _session.Cancel();
            if (!_valuesByDefinition.TryGetValue(value.Id, out var values))
            {
                values = value.Defaults;
                _valuesByDefinition[value.Id] = values;
            }

            _currentInstance = value.Build(values);
            _selectedComponent = value.DefaultSelectedComponent;
            ChartData = null;
            Cursor = null;
            RaiseDefinitionProperties();
            RebuildFieldsAndComponents();
            _ = RunAsync();
        }
    }

    /// <summary>Gets the current immutable demo instance.</summary>
    public DemoCircuitInstance CurrentInstance => _currentInstance;

    /// <summary>Gets the example description.</summary>
    public string Description => SelectedDefinition.Description;

    /// <summary>Gets or sets the selected component list item.</summary>
    public ComponentListItemViewModel? SelectedComponentItem
    {
        get => _selectedComponentItem;
        set
        {
            if (value is null || !SetProperty(ref _selectedComponentItem, value))
            {
                return;
            }

            SelectComponent(value.Key);
        }
    }

    /// <summary>Gets the selected stable component key.</summary>
    public DemoComponentKey SelectedComponent => _selectedComponent;

    /// <summary>Gets prepared chart data for the last successful current-example run.</summary>
    public TransientChartData? ChartData
    {
        get => _chartData;
        private set => SetProperty(ref _chartData, value);
    }

    /// <summary>Gets or sets the shared chart cursor.</summary>
    public ChartCursorSample? Cursor
    {
        get => _cursor;
        set
        {
            if (SetProperty(ref _cursor, value))
            {
                RaisePropertyChanged(nameof(CursorTime));
                RaisePropertyChanged(nameof(CursorVoltage));
                RaisePropertyChanged(nameof(CursorCurrent));
                RaisePropertyChanged(nameof(CursorPower));
            }
        }
    }

    /// <summary>Gets whether simulation is running.</summary>
    public bool IsRunning => _session.State == SimulationRunState.Running;

    /// <summary>Gets a concise state label.</summary>
    public string StatusText => _session.State switch
    {
        SimulationRunState.NotRun => "Ready",
        SimulationRunState.Running => "Simulating…",
        SimulationRunState.Completed => $"Completed · {_session.Result?.Samples.Count ?? 0:N0} samples",
        SimulationRunState.Cancelled => "Cancelled · previous result retained",
        SimulationRunState.Failed => "Simulation failed",
        _ => "Ready"
    };

    /// <summary>Gets the current diagnostic.</summary>
    public string? ErrorMessage => _session.ErrorMessage;

    /// <summary>Gets whether a simulation diagnostic should be shown.</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Gets selected component title.</summary>
    public string SelectedComponentTitle =>
        _currentInstance.Circuit.GetComponent(_currentInstance.GetComponentId(_selectedComponent)).Name;

    /// <summary>Gets selected component kind.</summary>
    public string SelectedComponentKind =>
        _currentInstance.Circuit.GetComponent(_currentInstance.GetComponentId(_selectedComponent)).Kind.ToString();

    /// <summary>Gets formatted cursor time.</summary>
    public string CursorTime => Cursor is null ? "—" : EngineeringNotation.Format(Cursor.Time, "s");

    /// <summary>Gets formatted cursor voltage.</summary>
    public string CursorVoltage => Cursor is null ? "—" : EngineeringNotation.Format(Cursor.Voltage, "V");

    /// <summary>Gets formatted cursor current.</summary>
    public string CursorCurrent => Cursor is null ? "—" : EngineeringNotation.Format(Cursor.Current, "A");

    /// <summary>Gets formatted cursor power.</summary>
    public string CursorPower => Cursor is null ? "—" : EngineeringNotation.Format(Cursor.Power, "W");

    /// <summary>Runs the initial RC example.</summary>
    public Task StartAsync() => RunAsync();

    /// <summary>Selects a component from the schematic or accessible list.</summary>
    public void SelectComponent(DemoComponentKey key)
    {
        if (_selectedComponent == key)
        {
            return;
        }

        _selectedComponent = key;
        _selectedComponentItem = Components.First(item => item.Key == key);
        RaisePropertyChanged(nameof(SelectedComponent));
        RaisePropertyChanged(nameof(SelectedComponentItem));
        RaisePropertyChanged(nameof(SelectedComponentTitle));
        RaisePropertyChanged(nameof(SelectedComponentKind));
        RebuildVisibleFields();
        RebuildChart();
    }

    /// <summary>Moves the chart cursor to the nearest sample.</summary>
    public void SetCursorTime(double time)
    {
        if (ChartData is not null)
        {
            Cursor = ChartData.FindNearest(time);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
        _session.Dispose();
    }

    private async Task ApplyAndRunAsync()
    {
        var allFields = BuildAllFields();
        var valid = true;
        var values = _valuesByDefinition[SelectedDefinition.Id];
        foreach (var field in allFields)
        {
            if (!field.TryGetValue(out var value))
            {
                valid = false;
                continue;
            }

            values = values.With(field.Definition, value);
        }

        if (!valid)
        {
            return;
        }

        if (values["run.stop"] / values["run.step"] > 200_000.0)
        {
            allFields.Single(field => field.Definition.Key == "run.step")
                .SetValidationError("Limit the simulation to 200,000 samples.");
            return;
        }

        _valuesByDefinition[SelectedDefinition.Id] = values;
        _currentInstance = SelectedDefinition.Build(values);
        RaisePropertyChanged(nameof(CurrentInstance));
        RebuildFieldsAndComponents();
        await RunAsync();
    }

    private async Task RunAsync() => await _session.RunAsync(_currentInstance);

    private void Reset()
    {
        _valuesByDefinition[SelectedDefinition.Id] = SelectedDefinition.Defaults;
        _currentInstance = SelectedDefinition.Build(SelectedDefinition.Defaults);
        RaisePropertyChanged(nameof(CurrentInstance));
        RebuildFieldsAndComponents();
        _ = RunAsync();
    }

    private void RebuildFieldsAndComponents()
    {
        Components.Clear();
        foreach (var placement in _currentInstance.Schematic.Components)
        {
            var component = _currentInstance.Circuit.GetComponent(placement.ComponentId);
            Components.Add(new ComponentListItemViewModel(placement.Key, component.Name, component.Kind));
        }

        _selectedComponentItem = Components.First(item => item.Key == _selectedComponent);
        RaisePropertyChanged(nameof(SelectedComponentItem));
        RaisePropertyChanged(nameof(SelectedComponentTitle));
        RaisePropertyChanged(nameof(SelectedComponentKind));
        RebuildVisibleFields();
    }

    private void RebuildVisibleFields()
    {
        ParameterFields.Clear();
        var values = _valuesByDefinition[SelectedDefinition.Id];
        foreach (var definition in SelectedDefinition.Parameters.Where(
                     parameter => parameter.Owner is null || parameter.Owner == _selectedComponent))
        {
            ParameterFields.Add(new ParameterFieldViewModel(definition, values[definition.Key]));
        }
    }

    private IReadOnlyList<ParameterFieldViewModel> BuildAllFields()
    {
        var visibleByKey = ParameterFields.ToDictionary(field => field.Definition.Key);
        var values = _valuesByDefinition[SelectedDefinition.Id];
        return SelectedDefinition.Parameters
            .Select(definition => visibleByKey.GetValueOrDefault(definition.Key)
                ?? new ParameterFieldViewModel(definition, values[definition.Key]))
            .ToArray();
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        if (_session.State == SimulationRunState.Completed &&
            _session.ResultInstance?.Definition.Id == SelectedDefinition.Id)
        {
            RebuildChart();
        }

        RaisePropertyChanged(nameof(IsRunning));
        RaisePropertyChanged(nameof(StatusText));
        RaisePropertyChanged(nameof(ErrorMessage));
        RaisePropertyChanged(nameof(HasErrorMessage));
        ApplyAndRunCommand.RaiseCanExecuteChanged();
        RunCommand.RaiseCanExecuteChanged();
        ResetCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    private void RebuildChart()
    {
        if (_session.Result is null ||
            _session.ResultInstance?.Definition.Id != SelectedDefinition.Id)
        {
            return;
        }

        ChartData = TransientChartData.Build(_session.ResultInstance, _session.Result, _selectedComponent);
        Cursor = ChartData.FindNearest(ChartData.StopTime);
    }

    private void RaiseDefinitionProperties()
    {
        RaisePropertyChanged(nameof(CurrentInstance));
        RaisePropertyChanged(nameof(Description));
        RaisePropertyChanged(nameof(SelectedComponent));
    }
}

/// <summary>Accessible component-list entry.</summary>
public sealed record ComponentListItemViewModel(
    DemoComponentKey Key,
    string Name,
    ComponentKind Kind)
{
    /// <summary>Gets a compact component label.</summary>
    public string Label => $"{Name}  ·  {Kind}";
}
