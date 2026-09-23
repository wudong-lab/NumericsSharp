using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

[TestClass]
public sealed class SparseMatrixAssemblerTests
{
    [TestMethod]
    public void ToCsr_ReusesFixedPatternWithoutSorting()
    {
        var patternBuilder = new SparseMatrixBuilder(3, 3);
        patternBuilder.AddSymmetric(0, 0, 1.0);
        patternBuilder.AddSymmetric(0, 1, 1.0);
        patternBuilder.AddSymmetric(1, 1, 1.0);
        patternBuilder.AddSymmetric(1, 2, 1.0);
        patternBuilder.AddSymmetric(2, 2, 1.0);

        var pattern = patternBuilder.ToCsr().Pattern;
        var assembler = new SparseMatrixAssembler(pattern);

        assembler.AddSymmetric(0, 0, 2.0);
        assembler.AddSymmetric(0, 1, -1.0);
        assembler.AddSymmetric(1, 1, 2.0);
        assembler.AddSymmetric(1, 2, -1.0);
        assembler.AddSymmetric(2, 2, 2.0);

        var matrix = assembler.ToCsr();

        CollectionAssert.AreEqual(new[] {0, 2, 5, 7}, matrix.RowOffsets);
        CollectionAssert.AreEqual(new[] {0, 1, 0, 1, 2, 1, 2}, matrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {2.0, -1.0, -1.0, 2.0, -1.0, -1.0, 2.0}, matrix.Values);
    }

    [TestMethod]
    public void ToCsr_SharesPatternWithAssembler()
    {
        var patternBuilder = new SparseMatrixBuilder(2, 2);
        patternBuilder.Add(0, 0, 1.0);
        patternBuilder.Add(1, 1, 1.0);

        var patternMatrix = patternBuilder.ToCsr();
        var pattern = patternMatrix.Pattern;
        var assembler = new SparseMatrixAssembler(pattern);

        assembler.Add(0, 0, 2.0);
        assembler.Add(1, 1, 3.0);

        var matrix = assembler.ToCsr();

        Assert.AreSame(pattern, matrix.Pattern);
        Assert.AreSame(pattern.RowOffsets, matrix.RowOffsets);
        Assert.AreSame(pattern.ColumnIndices, matrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {2.0, 3.0}, matrix.Values);
    }

    [TestMethod]
    public void Pattern_CloneDoesNotShareStructureArrays()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.Add(0, 0, 1.0);
        builder.Add(1, 1, 1.0);

        var pattern = builder.ToCsr().Pattern;
        var clone = pattern.Clone();

        Assert.AreNotSame(pattern, clone);
        Assert.AreNotSame(pattern.RowOffsets, clone.RowOffsets);
        Assert.AreNotSame(pattern.ColumnIndices, clone.ColumnIndices);

        clone.RowOffsets[1] = 0;
        clone.ColumnIndices[0] = 1;

        CollectionAssert.AreEqual(new[] {0, 1, 2}, pattern.RowOffsets);
        CollectionAssert.AreEqual(new[] {0, 1}, pattern.ColumnIndices);
    }

    [TestMethod]
    public void Add_ThrowsWhenEntryIsOutsidePattern()
    {
        var patternBuilder = new SparseMatrixBuilder(2, 2);
        patternBuilder.AddSymmetric(0, 0, 1.0);
        patternBuilder.AddSymmetric(1, 1, 1.0);

        var assembler = new SparseMatrixAssembler(patternBuilder.ToCsr().Pattern);

        Assert.ThrowsExactly<ArgumentException>(() => assembler.Add(0, 1, 1.0));
    }

    [TestMethod]
    public void Clear_AllowsRepeatedAssemblyWithSamePattern()
    {
        var patternBuilder = new SparseMatrixBuilder(2, 2);
        patternBuilder.AddSymmetric(0, 0, 1.0);
        patternBuilder.AddSymmetric(1, 1, 1.0);

        var assembler = new SparseMatrixAssembler(patternBuilder.ToCsr().Pattern);
        assembler.Add(0, 0, 2.0);
        assembler.Clear();
        assembler.Add(1, 1, 3.0);

        CollectionAssert.AreEqual(new[] {0.0, 3.0}, assembler.ToCsr().Values);
    }
}
