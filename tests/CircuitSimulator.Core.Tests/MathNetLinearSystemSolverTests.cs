using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class MathNetLinearSystemSolverTests
{
    [Fact]
    public void SolvesOneByOneAndTwoByTwoSystems()
    {
        var solver = new MathNetLinearSystemSolver();

        var oneByOne = solver.Solve(new MnaLinearSystem(new[,] { { 2.0 } }, [8.0]));
        var twoByTwo = solver.Solve(new MnaLinearSystem(
            new[,] { { 3.0, 2.0 }, { 1.0, 2.0 } },
            [7.0, 5.0]));

        AssertEx.NearlyEqual(4.0, oneByOne[0]);
        AssertEx.NearlyEqual(1.0, twoByTwo[0]);
        AssertEx.NearlyEqual(2.0, twoByTwo[1]);
    }

    [Fact]
    public void SolvesSystemRequiringPivoting()
    {
        var solver = new MathNetLinearSystemSolver();
        var solution = solver.Solve(new MnaLinearSystem(
            new[,] { { 0.0, 1.0 }, { 2.0, 3.0 } },
            [1.0, 5.0]));

        AssertEx.NearlyEqual(1.0, solution[0]);
        AssertEx.NearlyEqual(1.0, solution[1]);
    }

    [Fact]
    public void DetectsSingularAndNearSingularSystems()
    {
        var solver = new MathNetLinearSystemSolver();

        Assert.Throws<SingularMatrixException>(() => solver.Solve(new MnaLinearSystem(
            new[,] { { 1.0, 2.0 }, { 2.0, 4.0 } },
            [1.0, 2.0])));
        Assert.Throws<SingularMatrixException>(() => solver.Solve(new MnaLinearSystem(
            new[,] { { 1.0, 1.0 }, { 1.0, 1.0 + 1e-14 } },
            [1.0, 1.0])));
    }

    [Fact]
    public void DoesNotMutateInputSystem()
    {
        var matrix = new[,] { { 0.0, 1.0 }, { 2.0, 3.0 } };
        var rhs = new[] { 1.0, 5.0 };
        var system = new MnaLinearSystem(matrix, rhs);
        var solver = new MathNetLinearSystemSolver();

        solver.Solve(system);

        AssertEx.NearlyEqual(0.0, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(2.0, system.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(3.0, system.GetMatrixEntry(1, 1));
        AssertEx.NearlyEqual(1.0, system.GetRightHandSideEntry(0));
        AssertEx.NearlyEqual(5.0, system.GetRightHandSideEntry(1));
    }

    [Fact]
    public void HandlesZeroDimensionalSystem()
    {
        var solver = new MathNetLinearSystemSolver();
        var solution = solver.Solve(new MnaLinearSystem(new double[0, 0], []));

        Assert.Empty(solution);
    }

    [Fact]
    public void RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentException>(() => new MnaLinearSystem(new double[1, 2], [1.0]));
        Assert.Throws<ArgumentException>(() => new MnaLinearSystem(new double[1, 1], [1.0, 2.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MathNetLinearSystemSolver(double.NaN));
    }
}
