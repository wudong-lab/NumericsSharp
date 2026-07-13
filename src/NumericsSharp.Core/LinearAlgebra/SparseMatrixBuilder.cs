namespace NumericsSharp.Core.LinearAlgebra;

public sealed class SparseMatrixBuilder
{
    private readonly List<Entry> _entries;

    public SparseMatrixBuilder(int rowCount, int columnCount, int capacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this._entries = capacity > 0 ? new List<Entry>(capacity) : [];
    }

    public int RowCount { get; }
    public int ColumnCount { get; }
    public int EntryCount => this._entries.Count;

    public void Add(int row, int column, double value)
    {
        this.ThrowIfIndexOutOfRange(row, column);

        if (value == 0.0) return;

        this._entries.Add(new Entry(row, column, value));
    }

    public void AddSymmetric(int row, int column, double value)
    {
        this.Add(row, column, value);

        if (row != column)
        {
            this.Add(column, row, value);
        }
    }

    public void AddSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
        => this.AddSubmatrix(indices, indices, values);

    public void AddSubmatrix(ReadOnlySpan<int> rowIndices, ReadOnlySpan<int> columnIndices, ReadOnlySpan<double> values)
    {
        if (values.Length != rowIndices.Length * columnIndices.Length)
            throw new ArgumentException("Submatrix value count must equal rowIndices.Length * columnIndices.Length.", nameof(values));

        for (var localRow = 0; localRow < rowIndices.Length; localRow++)
        {
            var row = rowIndices[localRow];

            for (var localColumn = 0; localColumn < columnIndices.Length; localColumn++)
            {
                this.Add(row, columnIndices[localColumn], values[localRow * columnIndices.Length + localColumn]);
            }
        }
    }

    public void AddSymmetricSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
    {
        if (values.Length != indices.Length * indices.Length)
            throw new ArgumentException("Submatrix value count must equal indices.Length squared.", nameof(values));

        for (var localRow = 0; localRow < indices.Length; localRow++)
        {
            var row = indices[localRow];

            for (var localColumn = localRow; localColumn < indices.Length; localColumn++)
            {
                this.AddSymmetric(row, indices[localColumn], values[localRow * indices.Length + localColumn]);
            }
        }
    }

    public CsrMatrix ToCsr()
    {
        if (this._entries.Count == 0)
        {
            return new CsrMatrix(this.RowCount, this.ColumnCount, new int[this.RowCount + 1], [], []);
        }

        var entries = this._entries.ToArray();
        Array.Sort(entries, EntryComparer.Instance);

        var rowCounts = new int[this.RowCount];
        var columns = new List<int>(entries.Length);
        var values = new List<double>(entries.Length);

        var current = entries[0];
        var sum = current.Value;

        for (var i = 1; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.Row == current.Row && entry.Column == current.Column)
            {
                sum += entry.Value;
                continue;
            }

            AddMergedEntry(current.Row, current.Column, sum, rowCounts, columns, values);
            current = entry;
            sum = entry.Value;
        }

        AddMergedEntry(current.Row, current.Column, sum, rowCounts, columns, values);

        var rowOffsets = new int[this.RowCount + 1];
        for (var row = 0; row < this.RowCount; row++)
        {
            rowOffsets[row + 1] = rowOffsets[row] + rowCounts[row];
        }

        return new CsrMatrix(this.RowCount, this.ColumnCount, rowOffsets, columns.ToArray(), values.ToArray());
    }

    private void ThrowIfIndexOutOfRange(int row, int column)
    {
        if ((uint)row >= (uint)this.RowCount)
            throw new ArgumentOutOfRangeException(nameof(row));

        if ((uint)column >= (uint)this.ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(column));
    }

    private static void AddMergedEntry(
        int row,
        int column,
        double value,
        int[] rowCounts,
        List<int> columns,
        List<double> values)
    {
        if (value == 0.0) return;

        rowCounts[row]++;
        columns.Add(column);
        values.Add(value);
    }

    private readonly record struct Entry(int Row, int Column, double Value);

    private sealed class EntryComparer : IComparer<Entry>
    {
        public static readonly EntryComparer Instance = new();

        public int Compare(Entry x, Entry y)
        {
            var rowComparison = x.Row.CompareTo(y.Row);
            return rowComparison != 0 ? rowComparison : x.Column.CompareTo(y.Column);
        }
    }
}