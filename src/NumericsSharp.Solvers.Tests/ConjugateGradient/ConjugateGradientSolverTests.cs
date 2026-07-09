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
}
