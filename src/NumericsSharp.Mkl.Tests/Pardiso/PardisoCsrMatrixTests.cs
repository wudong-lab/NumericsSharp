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
}
