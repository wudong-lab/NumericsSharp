namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// Represents a sparse matrix in Compressed Sparse Row (CSR) format.
/// </summary>
public sealed class CsrMatrix : ILinearOperator
{
    public CsrMatrix(int rowCount, int columnCount, int[] rowOffsets, int[] columnIndices, double[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);

        if (rowOffsets.Length != rowCount + 1)
            throw new ArgumentException("CSR row offset count must equal rowCount + 1.", nameof(rowOffsets));

        if (columnIndices.Length != values.Length)
            throw new ArgumentException("CSR column index count must equal value count.", nameof(columnIndices));

        if (rowOffsets[0] != 0 || rowOffsets[^1] != values.Length)
            throw new ArgumentException("CSR row offsets are inconsistent with value count.", nameof(rowOffsets));

        for (var i = 0; i < rowOffsets.Length - 1; i++)
        {
            if (rowOffsets[i] > rowOffsets[i + 1])
                throw new ArgumentException("CSR row offsets must be nondecreasing.", nameof(rowOffsets));
        }

        foreach (var columnIndex in columnIndices)
        {
            if ((uint)columnIndex >= (uint)columnCount)
                throw new ArgumentOutOfRangeException(nameof(columnIndices), "CSR column index is out of range.");
        }

        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this.RowOffsets = rowOffsets;
        this.ColumnIndices = columnIndices;
        this.Values = values;
    }

    public int RowCount { get; }
    public int ColumnCount { get; }
    public int NonZeroCount => this.Values.Length;

    public int[] RowOffsets { get; }
    public int[] ColumnIndices { get; }
    public double[] Values { get; }

    public void CopyDiagonalTo(Span<double> diagonal)
    {
        if (diagonal.Length != Math.Min(this.RowCount, this.ColumnCount))
            throw new ArgumentException("Diagonal span length must equal min(rowCount, columnCount).", nameof(diagonal));

        diagonal.Clear();

        for (var row = 0; row < this.RowCount; row++)
        {
            var start = this.RowOffsets[row];
            var end = this.RowOffsets[row + 1];

            for (var index = start; index < end; index++)
            {
                if (this.ColumnIndices[index] == row)
                {
                    diagonal[row] = this.Values[index];
                    break;
                }
            }
        }
    }

    public void Multiply(ReadOnlySpan<double> x, Span<double> y)
    {
        if (x.Length != this.ColumnCount)
            throw new ArgumentException("Input vector length must equal matrix column count.", nameof(x));

        if (y.Length != this.RowCount)
            throw new ArgumentException("Output vector length must equal matrix row count.", nameof(y));

        for (var row = 0; row < this.RowCount; row++)
        {
            var sum = 0.0;
            var start = this.RowOffsets[row];
            var end = this.RowOffsets[row + 1];

            for (var index = start; index < end; index++)
            {
                sum += this.Values[index] * x[this.ColumnIndices[index]];
            }

            y[row] = sum;
        }
    }
}