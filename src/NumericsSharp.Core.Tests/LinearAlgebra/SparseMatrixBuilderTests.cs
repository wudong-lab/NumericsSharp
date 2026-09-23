using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

[TestClass]
public sealed class SparseMatrixBuilderTests
{
    [TestMethod]
    public void ToCsr_CombinesDuplicateEntries()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 1.0);
        builder.Add(0, 0, 2.0);
        builder.Add(1, 0, 3.0);

        var matrix = builder.ToCsr();

        Assert.AreEqual(2, matrix.NonZeroCount);
        CollectionAssert.AreEqual(new[] {0, 1, 2}, matrix.RowOffsets);
        CollectionAssert.AreEqual(new[] {0, 0}, matrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {3.0, 3.0}, matrix.Values);
    }

    [TestMethod]
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

        CollectionAssert.AreEqual(new[] {6.0, 7.0}, result);
    }

    [TestMethod]
    public void CopyDiagonalTo_CopiesMainDiagonal()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.Add(0, 0, 2.0);
        builder.Add(0, 2, 4.0);
        builder.Add(2, 2, 6.0);

        var matrix = builder.ToCsr();
        var diagonal = new double[3];

        matrix.CopyDiagonalTo(diagonal);

        CollectionAssert.AreEqual(new[] {2.0, 0.0, 6.0}, diagonal);
    }

    [TestMethod]
    public void GetValue_ReturnsStoredValueAndZeroForMissingEntry()
    {
        var builder = new SparseMatrixBuilder(2, 3);
        builder.Add(0, 1, 4.0);

        var matrix = builder.ToCsr();

        Assert.AreEqual(4.0, matrix.GetValue(0, 1));
        Assert.AreEqual(0.0, matrix.GetValue(1, 2));
    }

    [TestMethod]
    public void GetValue_ThrowsForOutOfRangeIndex()
    {
        var matrix = new SparseMatrixBuilder(2, 3).ToCsr();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => matrix.GetValue(2, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => matrix.GetValue(0, 3));
    }

    [TestMethod]
    public void CsrMatrix_RejectsUnsortedColumnIndices()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new CsrMatrix(
            2,
            2,
            [0, 2, 2],
            [1, 0],
            [1.0, 2.0]));
    }

    [TestMethod]
    public void AddSymmetricSubmatrix_ExpandsUpperTriangleToFullMatrix()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetricSubmatrix([0, 2], [2.0, -1.0, -1.0, 2.0]);

        var matrix = builder.ToCsr();

        CollectionAssert.AreEqual(new[] {0, 2, 2, 4}, matrix.RowOffsets);
        CollectionAssert.AreEqual(new[] {0, 2, 0, 2}, matrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {2.0, -1.0, -1.0, 2.0}, matrix.Values);
    }

    [TestMethod]
    public void AddSubmatrix_WithSeparateRowAndColumnIndices_AddsRectangularBlock()
    {
        var builder = new SparseMatrixBuilder(3, 4);
        builder.AddSubmatrix([0, 2], [1, 3], [1.0, 2.0, 3.0, 4.0]);

        var matrix = builder.ToCsr();

        CollectionAssert.AreEqual(new[] {0, 2, 2, 4}, matrix.RowOffsets);
        CollectionAssert.AreEqual(new[] {1, 3, 1, 3}, matrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {1.0, 2.0, 3.0, 4.0}, matrix.Values);
    }
}
