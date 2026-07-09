using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.ConjugateGradient;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.Tests.ConjugateGradient;

public sealed class ConjugateGradientSolverTests
{
    [Fact]
    public void Solve_ConvergesForSmallSpdSystem()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var rightHandSide = new[] { 1.0, 2.0 };
        var solution = new double[2];
        var solver = new ConjugateGradientSolver();

        var result = solver.Solve(matrix, rightHandSide, solution);

        Assert.True(result.Converged);
        Assert.InRange(Math.Abs(solution[0] - 1.0 / 11.0), 0.0, 1e-12);
        Assert.InRange(Math.Abs(solution[1] - 7.0 / 11.0), 0.0, 1e-12);
    }

    [Fact]
    public void Solve_ConvergesForLegacyDirectSparseSolveCase()
    {
        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var rightHandSide = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var expected = new[] { -326.3333333, 983.0, 163.4166667, 398.0, 61.5 };
        var solution = new double[5];
        var solver = new ConjugateGradientSolver();

        var result = solver.Solve(
            matrix,
            rightHandSide,
            solution,
            new ConjugateGradientOptions
            {
                MaxIterations = 100,
                RelativeTolerance = 1e-14
            });

        Assert.True(result.Converged);
        AssertEqual(expected, solution, 1e-5);
        Assert.InRange(LinearSystemResidual.ComputeL2Norm(matrix, solution, rightHandSide), 0.0, 1e-10);
    }

    [Fact]
    public void LinearSolver_SolvesThroughCommonSolverInterface()
    {
        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var rightHandSide = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var expected = new[] { -326.3333333, 983.0, 163.4166667, 398.0, 61.5 };
        var solution = new double[5];
        var solver = new ConjugateGradientLinearSolver(
            new ConjugateGradientOptions
            {
                MaxIterations = 100,
                RelativeTolerance = 1e-14
            });

        var result = solver.Solve(matrix, rightHandSide, solution);

        Assert.True(result.Converged);
        AssertEqual(expected, solution, 1e-5);
    }

    [Fact]
    public void Solve_WithJacobiPreconditioner_ConvergesForSmallSpdSystem()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var preconditioner = new JacobiPreconditioner(matrix);
        var rightHandSide = new[] { 1.0, 2.0 };
        var solution = new double[2];
        var solver = new PreconditionedConjugateGradientSolver();

        var result = solver.Solve(matrix, preconditioner, rightHandSide, solution);

        Assert.True(result.Converged);
        Assert.InRange(Math.Abs(solution[0] - 1.0 / 11.0), 0.0, 1e-12);
        Assert.InRange(Math.Abs(solution[1] - 7.0 / 11.0), 0.0, 1e-12);
    }

    [Fact]
    public void Solve_WithJacobiPreconditioner_ConvergesForLegacyDirectSparseSolveCase()
    {
        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var preconditioner = new JacobiPreconditioner(matrix);
        var rightHandSide = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var expected = new[] { -326.3333333, 983.0, 163.4166667, 398.0, 61.5 };
        var solution = new double[5];
        var solver = new PreconditionedConjugateGradientSolver();

        var result = solver.Solve(
            matrix,
            preconditioner,
            rightHandSide,
            solution,
            new ConjugateGradientOptions
            {
                MaxIterations = 100,
                RelativeTolerance = 1e-14
            });

        Assert.True(result.Converged);
        AssertEqual(expected, solution, 1e-5);
        Assert.InRange(LinearSystemResidual.ComputeL2Norm(matrix, solution, rightHandSide), 0.0, 1e-10);
    }

    [Fact]
    public void LinearSolver_UsesPreconditionerWhenProvided()
    {
        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var preconditioner = new JacobiPreconditioner(matrix);
        var rightHandSide = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var expected = new[] { -326.3333333, 983.0, 163.4166667, 398.0, 61.5 };
        var solution = new double[5];
        var solver = new ConjugateGradientLinearSolver(
            new ConjugateGradientOptions
            {
                MaxIterations = 100,
                RelativeTolerance = 1e-14
            },
            preconditioner);

        var result = solver.Solve(matrix, rightHandSide, solution);

        Assert.True(result.Converged);
        AssertEqual(expected, solution, 1e-5);
    }

    private static CsrMatrix CreateLegacyDirectSparseSolveMatrix()
    {
        var builder = new SparseMatrixBuilder(5, 5);

        builder.AddSymmetric(0, 0, 9.0);
        builder.AddSymmetric(1, 1, 0.5);
        builder.AddSymmetric(2, 2, 12.0);
        builder.AddSymmetric(3, 3, 0.625);
        builder.AddSymmetric(4, 4, 16.0);

        builder.AddSymmetric(0, 1, 1.5);
        builder.AddSymmetric(0, 2, 6.0);
        builder.AddSymmetric(0, 3, 0.75);
        builder.AddSymmetric(0, 4, 3.0);

        return builder.ToCsr();
    }

    private static void AssertEqual(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual, double tolerance)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.InRange(Math.Abs(actual[i] - expected[i]), 0.0, tolerance);
        }
    }
}
