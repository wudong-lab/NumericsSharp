using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl;
using NumericsSharp.Mkl.Native;
using NumericsSharp.Mkl.Tests.Native;
using NumericsSharp.Mkl.Pardiso;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Mkl.Tests.Pardiso;

public sealed class PardisoSolverTests
{
    [Fact]
    public void Analyze_AcceptsSquareCsrMatrix()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = new PardisoSolver();

        solver.Analyze(builder.ToCsr());

        Assert.True(solver.IsAnalyzed);
        Assert.False(solver.IsFactorized);
    }

    [Fact]
    public void PardisoSolver_ImplementsDirectSparseSolverInterface()
    {
        using IDirectSparseSolver solver = new PardisoSolver();

        Assert.False(solver.IsAnalyzed);
        Assert.False(solver.IsFactorized);
    }

    [Fact]
    public void Solve_ComputesSmallSpdSystemWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var solution = new double[2];
        using var solver = new PardisoSolver();

        try
        {
            var result = solver.Solve(matrix, [1.0, 2.0], solution);

            Assert.True(result.Converged);
            Assert.True(solver.IsAnalyzed);
            Assert.True(solver.IsFactorized);
            Assert.InRange(Math.Abs(solution[0] - 1.0 / 11.0), 0.0, 1e-12);
            Assert.InRange(Math.Abs(solution[1] - 7.0 / 11.0), 0.0, 1e-12);
            Assert.InRange(result.FinalResidualNorm, 0.0, 1e-12);
        }
        catch (MklBackendException exception)
        {
            Assert.Equal((int)MklNativeStatus.MklError, exception.StatusCode);
        }
    }

    [Fact]
    public void Solve_ComputesLegacySparseSolveCaseWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var solution = new double[5];
        using var solver = new PardisoSolver();

        var result = solver.Solve(matrix, [1.0, 2.0, 3.0, 4.0, 5.0], solution);

        Assert.True(result.Converged);
        AssertEqual([-326.3333333, 983.0, 163.4166667, 398.0, 61.5], solution, 1e-5);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-9);
    }

    [Fact]
    public void Factorize_UpdatesStateWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = new PardisoSolver();

        try
        {
            solver.Factorize(builder.ToCsr());

            Assert.True(solver.IsAnalyzed);
            Assert.True(solver.IsFactorized);
        }
        catch (MklBackendException exception)
        {
            Assert.Equal((int)MklNativeStatus.MklError, exception.StatusCode);
            Assert.Equal(nameof(MklNativeStatus.MklError), exception.StatusName);
            Assert.True(solver.IsAnalyzed);
            Assert.False(solver.IsFactorized);
        }
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
