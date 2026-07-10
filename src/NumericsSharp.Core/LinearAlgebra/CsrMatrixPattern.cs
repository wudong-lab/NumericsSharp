namespace NumericsSharp.Core.LinearAlgebra;

public sealed class CsrMatrixPattern
{
    private CsrMatrixPattern(int rowCount, int columnCount, int[] rowOffsets, int[] columnIndices)
    {
        RowCount = rowCount;
        ColumnCount = columnCount;
        RowOffsets = rowOffsets;
        ColumnIndices = columnIndices;
    }

    public int RowCount { get; }

    public int ColumnCount { get; }

    public int NonZeroCount => ColumnIndices.Length;

    public int[] RowOffsets { get; }

    public int[] ColumnIndices { get; }

    public static CsrMatrixPattern FromCsr(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        return new CsrMatrixPattern(
            matrix.RowCount,
            matrix.ColumnCount,
            (int[])matrix.RowOffsets.Clone(),
            (int[])matrix.ColumnIndices.Clone());
    }

    public double[] CreateValueBuffer() => new double[NonZeroCount];

    public CsrMatrix ToCsr(ReadOnlySpan<double> values)
    {
        if (values.Length != NonZeroCount)
        {
            throw new ArgumentException("Value count must equal pattern nonzero count.", nameof(values));
        }

        return new CsrMatrix(
            RowCount,
            ColumnCount,
            (int[])RowOffsets.Clone(),
            (int[])ColumnIndices.Clone(),
            values.ToArray());
    }

    public int FindEntryIndex(int row, int column)
    {
        ThrowIfIndexOutOfRange(row, column);

        var start = RowOffsets[row];
        var count = RowOffsets[row + 1] - start;
        var offset = Array.BinarySearch(ColumnIndices, start, count, column);

        if (offset < 0)
        {
            throw new ArgumentException("CSR pattern does not contain the requested matrix entry.");
        }

        return offset;
    }

    private void ThrowIfIndexOutOfRange(int row, int column)
    {
        if ((uint)row >= (uint)RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if ((uint)column >= (uint)ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
    }
}
