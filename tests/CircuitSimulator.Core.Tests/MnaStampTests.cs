using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class MnaStampTests
{
    [Fact]
    public void ConductanceBetweenTwoNodesStampsExactMatrixEntries()
    {
        var builder = new DenseMnaSystemBuilder(2);

        MnaStamps.StampConductance(builder, new VariableIndex(0), new VariableIndex(1), 2.0);
        var system = builder.Build();

        AssertEx.NearlyEqual(2.0, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(-2.0, system.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(-2.0, system.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(2.0, system.GetMatrixEntry(1, 1));
    }

    [Fact]
    public void ConductanceToGroundStampsOnlyExistingNode()
    {
        var builder = new DenseMnaSystemBuilder(1);

        MnaStamps.StampConductance(builder, new VariableIndex(0), null, 3.0);
        var system = builder.Build();

        AssertEx.NearlyEqual(3.0, system.GetMatrixEntry(0, 0));
    }

    [Fact]
    public void CurrentSourceBetweenNodesUsesInjectionConvention()
    {
        var builder = new DenseMnaSystemBuilder(2);

        MnaStamps.StampCurrentSource(builder, new VariableIndex(0), new VariableIndex(1), 5.0);
        var system = builder.Build();

        AssertEx.NearlyEqual(-5.0, system.GetRightHandSideEntry(0));
        AssertEx.NearlyEqual(5.0, system.GetRightHandSideEntry(1));
    }

    [Fact]
    public void CurrentSourceFromGroundToNodeInjectsPositiveCurrentIntoNode()
    {
        var builder = new DenseMnaSystemBuilder(1);

        MnaStamps.StampCurrentSource(builder, null, new VariableIndex(0), 0.25);
        var system = builder.Build();

        AssertEx.NearlyEqual(0.25, system.GetRightHandSideEntry(0));
    }

    [Fact]
    public void VoltageSourceBetweenNodesStampsConstraintAndBranchColumn()
    {
        var builder = new DenseMnaSystemBuilder(3);

        MnaStamps.StampVoltageSource(
            builder,
            new VariableIndex(0),
            new VariableIndex(1),
            new VariableIndex(2),
            7.0);
        var system = builder.Build();

        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(0, 2));
        AssertEx.NearlyEqual(-1.0, system.GetMatrixEntry(1, 2));
        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(2, 0));
        AssertEx.NearlyEqual(-1.0, system.GetMatrixEntry(2, 1));
        AssertEx.NearlyEqual(7.0, system.GetRightHandSideEntry(2));
    }

    [Fact]
    public void ContributionsAccumulate()
    {
        var builder = new DenseMnaSystemBuilder(1);

        MnaStamps.StampConductance(builder, new VariableIndex(0), null, 2.0);
        MnaStamps.StampConductance(builder, new VariableIndex(0), null, 3.0);
        MnaStamps.StampCurrentSource(builder, null, new VariableIndex(0), 4.0);
        MnaStamps.StampCurrentSource(builder, null, new VariableIndex(0), 5.0);
        var system = builder.Build();

        AssertEx.NearlyEqual(5.0, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(9.0, system.GetRightHandSideEntry(0));
    }

    [Fact]
    public void DenseBuilderRejectsInvalidIndexesAndNonFiniteValues()
    {
        var builder = new DenseMnaSystemBuilder(1);

        Assert.Throws<MnaAssemblyException>(() => builder.AddMatrix(new VariableIndex(1), new VariableIndex(0), 1.0));
        Assert.Throws<MnaAssemblyException>(() => builder.AddRightHandSide(new VariableIndex(0), double.NaN));
        Assert.Throws<MnaAssemblyException>(() => MnaStamps.StampConductance(builder, new VariableIndex(0), null, double.PositiveInfinity));
    }

    [Fact]
    public void LinearSystemDefensivelyCopiesArrays()
    {
        var matrix = new[,] { { 2.0 } };
        var rhs = new[] { 1.0 };
        var system = new MnaLinearSystem(matrix, rhs);

        matrix[0, 0] = 99.0;
        rhs[0] = 99.0;
        var matrixCopy = system.Matrix;
        var rhsCopy = system.RightHandSide;
        matrixCopy[0, 0] = 88.0;
        rhsCopy[0] = 88.0;

        AssertEx.NearlyEqual(2.0, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(1.0, system.GetRightHandSideEntry(0));
    }
}
