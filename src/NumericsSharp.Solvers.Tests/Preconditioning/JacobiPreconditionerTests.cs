using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.Tests.Preconditioning;

public sealed class JacobiPreconditionerTests
{
    [Fact]
    public void Apply_MultipliesResidualByInverseDiagonal()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 2.0);
        builder.Add(1, 1, 4.0);

        var preconditioner = new JacobiPreconditioner(builder.ToCsr());
        var result = new double[2];

        preconditioner.Apply([2.0, 8.0], result);

        Assert.Equal([1.0, 2.0], result);
    }
}
