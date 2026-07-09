using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

public sealed class DirichletBoundaryConditionTests
{
    [Fact]
    public void Apply_EliminatesConstrainedRowsAndColumnsAndAdjustsRightHandSide()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(0, 1, -1.0);
        builder.AddSymmetric(1, 1, 2.0);
        builder.AddSymmetric(1, 2, -1.0);
        builder.AddSymmetric(2, 2, 2.0);

        var matrix = builder.ToCsr();
        var rightHandSide = new[] { 0.0, 0.0, 0.0 };

        var constrainedMatrix = DirichletBoundaryCondition.Apply(matrix, rightHandSide, [0], [10.0]);

        Assert.Equal([0, 1, 3, 5], constrainedMatrix.RowOffsets);
        Assert.Equal([0, 1, 2, 1, 2], constrainedMatrix.ColumnIndices);
        Assert.Equal([1.0, 2.0, -1.0, -1.0, 2.0], constrainedMatrix.Values);
        Assert.Equal([10.0, 10.0, 0.0], rightHandSide);
    }
}
