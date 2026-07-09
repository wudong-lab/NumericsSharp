using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

public sealed class SparseMatrixBuilderTests
{
    [Fact]
    public void ToCsr_CombinesDuplicateEntries()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 1.0);
        builder.Add(0, 0, 2.0);
        builder.Add(1, 0, 3.0);

        var matrix = builder.ToCsr();

        Assert.Equal(2, matrix.NonZeroCount);
        Assert.Equal([0, 1, 2], matrix.RowOffsets);
        Assert.Equal([0, 0], matrix.ColumnIndices);
        Assert.Equal([3.0, 3.0], matrix.Values);
    }

    [Fact]
    public void Multiply_ComputesMatrixVectorProduct()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 4.0);
        builder.Add(0, 1, 1.0);
        builder.Add(1, 0, 1.0);
        builder.Add(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var result = new double[2];

        matrix.Multiply([1.0, 2.0], result);

        Assert.Equal([6.0, 7.0], result);
    }

    [Fact]
    public void CopyDiagonalTo_CopiesMainDiagonal()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.Add(0, 0, 2.0);
        builder.Add(0, 2, 4.0);
        builder.Add(2, 2, 6.0);

        var matrix = builder.ToCsr();
        var diagonal = new double[3];

        matrix.CopyDiagonalTo(diagonal);

        Assert.Equal([2.0, 0.0, 6.0], diagonal);
    }

    [Fact]
    public void AddSymmetricSubmatrix_ExpandsUpperTriangleToFullMatrix()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetricSubmatrix([0, 2], [2.0, -1.0, -1.0, 2.0]);

        var matrix = builder.ToCsr();

        Assert.Equal([0, 2, 2, 4], matrix.RowOffsets);
        Assert.Equal([0, 2, 0, 2], matrix.ColumnIndices);
        Assert.Equal([2.0, -1.0, -1.0, 2.0], matrix.Values);
    }

    [Fact]
    public void AddSubmatrix_WithSeparateRowAndColumnIndices_AddsRectangularBlock()
    {
        var builder = new SparseMatrixBuilder(3, 4);
        builder.AddSubmatrix([0, 2], [1, 3], [1.0, 2.0, 3.0, 4.0]);

        var matrix = builder.ToCsr();

        Assert.Equal([0, 2, 2, 4], matrix.RowOffsets);
        Assert.Equal([1, 3, 1, 3], matrix.ColumnIndices);
        Assert.Equal([1.0, 2.0, 3.0, 4.0], matrix.Values);
    }
}
