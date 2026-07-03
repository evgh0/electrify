using System;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Numerics;
using NUnit.Framework;

namespace CircuitSimulator.Unity.Tests
{
    public sealed class DenseLinearSystemSolverTests
    {
        [Test]
        public void SolveUsesPivotRowSwap()
        {
            var solver = new DenseLinearSystemSolver();
            var system = new MnaLinearSystem(
                new double[,]
                {
                    { 0.0, 2.0 },
                    { 1.0, 1.0 },
                },
                new[] { 4.0, 3.0 });

            var solution = solver.Solve(system);

            Assert.That(solution[0], Is.EqualTo(1.0).Within(1e-12));
            Assert.That(solution[1], Is.EqualTo(2.0).Within(1e-12));
        }

        [Test]
        public void SolveDoesNotMutateInputSystem()
        {
            var matrix = new double[,]
            {
                { 2.0, 1.0 },
                { 1.0, 3.0 },
            };
            var rightHandSide = new[] { 1.0, 2.0 };
            var system = new MnaLinearSystem(matrix, rightHandSide);

            _ = new DenseLinearSystemSolver().Solve(system);

            Assert.That(matrix[0, 0], Is.EqualTo(2.0));
            Assert.That(matrix[0, 1], Is.EqualTo(1.0));
            Assert.That(matrix[1, 0], Is.EqualTo(1.0));
            Assert.That(matrix[1, 1], Is.EqualTo(3.0));
            Assert.That(rightHandSide, Is.EqualTo(new[] { 1.0, 2.0 }));
            Assert.That(system.GetMatrixEntry(0, 0), Is.EqualTo(2.0));
            Assert.That(system.GetMatrixEntry(0, 1), Is.EqualTo(1.0));
            Assert.That(system.GetMatrixEntry(1, 0), Is.EqualTo(1.0));
            Assert.That(system.GetMatrixEntry(1, 1), Is.EqualTo(3.0));
            Assert.That(system.GetRightHandSideEntry(0), Is.EqualTo(1.0));
            Assert.That(system.GetRightHandSideEntry(1), Is.EqualTo(2.0));
        }

        [Test]
        public void SolveZeroDimensionalSystemReturnsEmptySolution()
        {
            var system = new MnaLinearSystem(new double[0, 0], Array.Empty<double>());

            var solution = new DenseLinearSystemSolver().Solve(system);

            Assert.That(solution, Is.Empty);
        }

        [Test]
        public void SolveDetectsSingularMatrix()
        {
            var system = new MnaLinearSystem(
                new double[,]
                {
                    { 1.0, 2.0 },
                    { 2.0, 4.0 },
                },
                new[] { 3.0, 6.0 });

            Assert.Throws<SingularMatrixException>(() => new DenseLinearSystemSolver().Solve(system));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void ConstructorRejectsInvalidPivotTolerance(double pivotTolerance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DenseLinearSystemSolver(pivotTolerance));
        }

        [Test]
        public void SolveDetectsNonFiniteSolution()
        {
            var system = new MnaLinearSystem(
                new double[,] { { double.Epsilon } },
                new[] { double.MaxValue });

            var exception = Assert.Throws<SingularMatrixException>(
                () => new DenseLinearSystemSolver(0.0).Solve(system));

            Assert.That(exception.Message, Does.Contain("non-finite solution"));
        }
    }
}
