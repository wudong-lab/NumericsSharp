using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

public sealed class LinearSystemResidualTests
{
    [Fact]
    public void ComputeL2Norm_ReturnsZeroForExactSolution()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 4.0);
        builder.Add(0, 1, 1.0);
        builder.Add(1, 0, 1.0);
        builder.Add(1, 1, 3.0);

        var matrix = builder.ToCsr();

        var residualNorm = LinearSystemResidual.ComputeL2Norm(matrix, [1.0 / 11.0, 7.0 / 11.0], [1.0, 2.0]);

        Assert.InRange(residualNorm, 0.0, 1e-14);
    }
}
