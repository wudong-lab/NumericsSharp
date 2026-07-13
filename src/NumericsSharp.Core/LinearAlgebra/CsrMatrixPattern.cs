namespace NumericsSharp.Core.LinearAlgebra;

public sealed class CsrMatrixPattern
{
    private CsrMatrixPattern(int rowCount, int columnCount, int[] rowOffsets, int[] columnIndices)
    {
        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this.RowOffsets = rowOffsets;
        this.ColumnIndices = columnIndices;
    }

    public int RowCount { get; }
    public int ColumnCount { get; }
    public int NonZeroCount => this.ColumnIndices.Length;

    public int[] RowOffsets { get; }
    public int[] ColumnIndices { get; }

    public static CsrMatrixPattern FromCsr(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        return new CsrMatrixPattern(matrix.RowCount, matrix.ColumnCount,
            (int[])matrix.RowOffsets.Clone(), (int[])matrix.ColumnIndices.Clone());
    }

    public double[] CreateValueBuffer() => new double[this.NonZeroCount];

    public CsrMatrix ToCsr(ReadOnlySpan<double> values)
    {
        if (values.Length != this.NonZeroCount)
            throw new ArgumentException("Value count must equal pattern nonzero count.", nameof(values));

        return new CsrMatrix(this.RowCount, this.ColumnCount,
            (int[])this.RowOffsets.Clone(),
            (int[])this.ColumnIndices.Clone(),
            values.ToArray());
    }

    public int FindEntryIndex(int row, int column)
    {
        this.ThrowIfIndexOutOfRange(row, column);

        var start = this.RowOffsets[row];
        var count = this.RowOffsets[row + 1] - start;
        var offset = Array.BinarySearch(this.ColumnIndices, start, count, column);

        if (offset < 0)
            throw new ArgumentException("CSR pattern does not contain the requested matrix entry.");

        return offset;
    }

    private void ThrowIfIndexOutOfRange(int row, int column)
    {
        if ((uint)row >= (uint)this.RowCount)
            throw new ArgumentOutOfRangeException(nameof(row));

        if ((uint)column >= (uint)this.ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(column));
    }
}