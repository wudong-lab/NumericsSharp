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
    public void Solve_ThrowsUntilNativeBackendIsImplemented()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = new PardisoSolver();

        Assert.Throws<PlatformNotSupportedException>(() => solver.Solve(builder.ToCsr(), [1.0, 2.0], new double[2]));
    }

    [Fact]
    public void Factorize_PropagatesNativeFailureUntilMklIsImplemented()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = new PardisoSolver();

        var exception = Assert.Throws<MklBackendException>(() => solver.Factorize(builder.ToCsr()));

        Assert.Equal((int)MklNativeStatus.MklError, exception.StatusCode);
        Assert.Equal(nameof(MklNativeStatus.MklError), exception.StatusName);
        Assert.True(solver.IsAnalyzed);
        Assert.False(solver.IsFactorized);
    }
}
