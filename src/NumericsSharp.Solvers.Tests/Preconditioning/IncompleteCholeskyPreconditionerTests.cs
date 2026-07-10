using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.Tests.Preconditioning;

public sealed class IncompleteCholeskyPreconditionerTests
{
    [Fact]
    public void Apply_SolvesExactCholeskyForSmallDenseSpdMatrix()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var preconditioner = new IncompleteCholeskyPreconditioner(builder.ToCsr());
        var result = new double[2];

        preconditioner.Apply([1.0, 2.0], result);

        Assert.InRange(Math.Abs(result[0] - 1.0 / 11.0), 0.0, 1e-12);
        Assert.InRange(Math.Abs(result[1] - 7.0 / 11.0), 0.0, 1e-12);
    }
}
