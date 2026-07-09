using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Pardiso;

public sealed class PardisoCsrMatrixTests
{
    [Fact]
    public void FromCsr_ConvertsZeroBasedCsrToOneBasedPardisoArrays()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.Add(0, 0, 2.0);
        builder.Add(0, 2, -1.0);
        builder.Add(2, 1, 4.0);

        var matrix = PardisoCsrMatrix.FromCsr(builder.ToCsr());

        Assert.Equal(3, matrix.Order);
        Assert.Equal(3, matrix.NonZeroCount);
        Assert.Equal([1, 3, 3, 4], matrix.RowPointers);
        Assert.Equal([1, 3, 2], matrix.Columns);
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
        Assert.Equal([1, 4, 6, 7], matrix.RowPointers);
        Assert.Equal([1, 2, 3, 2, 3, 3], matrix.Columns);
        Assert.Equal([2.0, -1.0, 0.5, 3.0, 4.0, 5.0], matrix.Values);
    }
}
