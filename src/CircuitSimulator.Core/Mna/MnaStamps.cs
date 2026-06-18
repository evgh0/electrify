using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Provides reusable ground-aware MNA stamp helpers.
/// </summary>
public static class MnaStamps
{
    /// <summary>
    /// Stamps a conductance between two node-voltage variables.
    /// </summary>
    /// <param name="builder">The MNA builder.</param>
    /// <param name="positiveNodeVoltage">The positive node-voltage variable, or null for ground.</param>
    /// <param name="negativeNodeVoltage">The negative node-voltage variable, or null for ground.</param>
    /// <param name="conductance">The finite conductance in siemens.</param>
    public static void StampConductance(
        IMnaSystemBuilder builder,
        VariableIndex? positiveNodeVoltage,
        VariableIndex? negativeNodeVoltage,
        double conductance)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateFinite(conductance, nameof(conductance));

        if (positiveNodeVoltage is { } p)
        {
            builder.AddMatrix(p, p, conductance);
        }

        if (negativeNodeVoltage is { } n)
        {
            builder.AddMatrix(n, n, conductance);
        }

        if (positiveNodeVoltage is { } p2 && negativeNodeVoltage is { } n2)
        {
            builder.AddMatrix(p2, n2, -conductance);
            builder.AddMatrix(n2, p2, -conductance);
        }
    }

    /// <summary>
    /// Stamps a current source using positive current from positive node to negative node.
    /// </summary>
    /// <param name="builder">The MNA builder.</param>
    /// <param name="positiveNodeVoltage">The positive node-voltage variable, or null for ground.</param>
    /// <param name="negativeNodeVoltage">The negative node-voltage variable, or null for ground.</param>
    /// <param name="current">The finite current in amperes.</param>
    public static void StampCurrentSource(
        IMnaSystemBuilder builder,
        VariableIndex? positiveNodeVoltage,
        VariableIndex? negativeNodeVoltage,
        double current)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateFinite(current, nameof(current));

        if (positiveNodeVoltage is { } p)
        {
            builder.AddRightHandSide(p, -current);
        }

        if (negativeNodeVoltage is { } n)
        {
            builder.AddRightHandSide(n, current);
        }
    }

    /// <summary>
    /// Stamps an independent voltage source constraint.
    /// </summary>
    /// <param name="builder">The MNA builder.</param>
    /// <param name="positiveNodeVoltage">The positive node-voltage variable, or null for ground.</param>
    /// <param name="negativeNodeVoltage">The negative node-voltage variable, or null for ground.</param>
    /// <param name="branchCurrent">The branch-current variable.</param>
    /// <param name="voltage">The finite voltage in volts.</param>
    public static void StampVoltageSource(
        IMnaSystemBuilder builder,
        VariableIndex? positiveNodeVoltage,
        VariableIndex? negativeNodeVoltage,
        VariableIndex branchCurrent,
        double voltage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateFinite(voltage, nameof(voltage));

        if (positiveNodeVoltage is { } p)
        {
            builder.AddMatrix(p, branchCurrent, 1.0);
            builder.AddMatrix(branchCurrent, p, 1.0);
        }

        if (negativeNodeVoltage is { } n)
        {
            builder.AddMatrix(n, branchCurrent, -1.0);
            builder.AddMatrix(branchCurrent, n, -1.0);
        }

        builder.AddRightHandSide(branchCurrent, voltage);
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new MnaAssemblyException($"MNA stamp parameter '{parameterName}' must be finite.");
        }
    }
}
