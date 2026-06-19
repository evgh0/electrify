using CircuitSimulator.Editor.Documents;
using CircuitSimulator.Editor.Properties;

namespace CircuitSimulator.Editor.ViewModels;

/// <summary>Editable engineering-notation field retaining its last valid value.</summary>
public sealed class ParameterFieldViewModel : BindableBase
{
    private string _text;
    private string? _error;

    /// <summary>Initializes a parameter field.</summary>
    public ParameterFieldViewModel(DemoParameterDefinition definition, double value)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _text = EngineeringNotation.Format(value);
    }

    /// <summary>Gets parameter metadata.</summary>
    public DemoParameterDefinition Definition { get; }

    /// <summary>Gets its label.</summary>
    public string DisplayName => Definition.DisplayName;

    /// <summary>Gets its unit.</summary>
    public string Unit => Definition.Unit;

    /// <summary>Gets or sets uncommitted input text.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                Error = null;
            }
        }
    }

    /// <summary>Gets an inline validation message.</summary>
    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                RaisePropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>Gets whether validation failed.</summary>
    public bool HasError => Error is not null;

    /// <summary>Validates and returns the SI value without mutating the circuit.</summary>
    public bool TryGetValue(out double value)
    {
        if (!EngineeringNotation.TryParse(Text, out value))
        {
            Error = "Enter a valid engineering value.";
            return false;
        }

        if (!Definition.IsValid(value))
        {
            Error = Definition.MinimumExclusive >= 0.0
                ? "Value must be greater than zero."
                : "Value is outside the supported range.";
            return false;
        }

        Error = null;
        return true;
    }

    /// <summary>Sets a cross-field validation error.</summary>
    public void SetValidationError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Error = message;
    }
}
