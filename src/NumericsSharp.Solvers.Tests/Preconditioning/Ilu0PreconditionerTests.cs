using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.Tests.Preconditioning;

public sealed class Ilu0PreconditionerTests
{
    [Fact]
    public void Apply_SolvesExactLuForSmallDenseMatrix()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 4.0);
        builder.Add(0, 1, 1.0);
        builder.Add(1, 0, 2.0);
        builder.Add(1, 1, 3.0);

        var preconditioner = new Ilu0Preconditioner(builder.ToCsr());
        var result = new double[2];

        preconditioner.Apply([1.0, 2.0], result);

        Assert.InRange(Math.Abs(result[0] - 0.1), 0.0, 1e-12);
        Assert.InRange(Math.Abs(result[1] - 0.6), 0.0, 1e-12);
    }
}
