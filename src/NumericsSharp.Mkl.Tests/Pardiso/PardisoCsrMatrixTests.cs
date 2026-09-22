using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Pardiso;

public sealed class PardisoCsrMatrixTests
{
    [Fact]
    public void FromCsr_PreservesZeroBasedCsrArrays()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.Add(0, 0, 2.0);
        builder.Add(0, 2, -1.0);
        builder.Add(2, 1, 4.0);

        var matrix = PardisoCsrMatrix.FromCsr(builder.ToCsr());

        Assert.Equal(3, matrix.Order);
        Assert.Equal(3, matrix.NonZeroCount);
        Assert.Equal([0, 2, 2, 3], matrix.RowPointers);
        Assert.Equal([0, 2, 1], matrix.Columns);
        Assert.Equal([2.0, -1.0, 4.0], matrix.Values);
    }

    [Fact]
    public void FromCsr_ForSymmetricMatrixType_KeepsUpperTriangleOnly()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(0, 1, -1.0);
        builder.AddSymmetric(0, 2, 0.5);
        builder.AddSymmetric(1, 1, 3.0);
        builder.AddSymmetric(1, 2, 4.0);
        builder.AddSymmetric(2, 2, 5.0);

        var matrix = PardisoCsrMatrix.FromCsr(
            builder.ToCsr(),
            PardisoMatrixType.RealSymmetricPositiveDefinite);

        Assert.Equal(3, matrix.Order);
        Assert.Equal(6, matrix.NonZeroCount);
        Assert.Equal([0, 3, 5, 6], matrix.RowPointers);
        Assert.Equal([0, 1, 2, 1, 2, 2], matrix.Columns);
        Assert.Equal([2.0, -1.0, 0.5, 3.0, 4.0, 5.0], matrix.Values);
    }

    [Fact]
    public void TryUpdateValues_ReusesUpperTriangleStructure()
    {
        var firstMatrix = CreateSymmetricMatrix(2.0, -1.0, 3.0);
        var secondMatrix = CreateSymmetricMatrix(5.0, 4.0, 7.0);
        var adaptedMatrix = PardisoCsrMatrix.FromCsr(
            firstMatrix,
            PardisoMatrixType.RealSymmetricPositiveDefinite);
        var rowPointers = adaptedMatrix.RowPointers;
        var columns = adaptedMatrix.Columns;

        Assert.True(adaptedMatrix.TryUpdateValues(secondMatrix));

        Assert.Same(rowPointers, adaptedMatrix.RowPointers);
        Assert.Same(columns, adaptedMatrix.Columns);
        Assert.Equal([5.0, 4.0, 7.0], adaptedMatrix.Values);
    }

    [Fact]
    public void TryUpdateValues_RejectsDifferentUpperTriangleStructure()
    {
        var firstMatrix = CreateSymmetricMatrix(2.0, -1.0, 3.0);
        var differentMatrixBuilder = new SparseMatrixBuilder(2, 2);
        differentMatrixBuilder.AddSymmetric(0, 0, 2.0);
        differentMatrixBuilder.AddSymmetric(1, 1, 3.0);
        var adaptedMatrix = PardisoCsrMatrix.FromCsr(
            firstMatrix,
            PardisoMatrixType.RealSymmetricPositiveDefinite);
        var originalValues = adaptedMatrix.Values.ToArray();

        Assert.False(adaptedMatrix.TryUpdateValues(differentMatrixBuilder.ToCsr()));
        Assert.Equal(originalValues, adaptedMatrix.Values);
    }

    private static CsrMatrix CreateSymmetricMatrix(double diagonal0, double offDiagonal, double diagonal1)
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, diagonal0);
        builder.AddSymmetric(0, 1, offDiagonal);
        builder.AddSymmetric(1, 1, diagonal1);
        return builder.ToCsr();
    }
}
