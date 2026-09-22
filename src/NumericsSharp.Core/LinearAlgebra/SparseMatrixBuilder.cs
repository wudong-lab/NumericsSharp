namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 以条目追加方式构造稀疏矩阵，并在转换时生成 CSR 表示。
/// </summary>
public sealed class SparseMatrixBuilder
{
    private readonly List<Entry> _entries;

    /// <summary>
    /// 创建稀疏矩阵构造器。
    /// </summary>
    /// <param name="rowCount">矩阵的行数。</param>
    /// <param name="columnCount">矩阵的列数。</param>
    /// <param name="capacity">内部条目列表的初始容量。</param>
    /// <exception cref="ArgumentOutOfRangeException">行数、列数或容量为负数或零（行数和列数）时抛出。</exception>
    public SparseMatrixBuilder(int rowCount, int columnCount, int capacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this._entries = capacity > 0 ? new List<Entry>(capacity) : [];
    }

    /// <summary>
    /// 获取矩阵的行数。
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    /// 获取矩阵的列数。
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// 获取当前已追加的条目数，包括尚未合并的重复位置。
    /// </summary>
    public int EntryCount => this._entries.Count;

    /// <summary>
    /// 追加一个矩阵条目。数值为零的条目会被忽略。
    /// </summary>
    /// <param name="row">条目行索引。</param>
    /// <param name="column">条目列索引。</param>
    /// <param name="value">条目值。</param>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    public void Add(int row, int column, double value)
    {
        this.ThrowIfIndexOutOfRange(row, column);

        if (value == 0.0) return;

        this._entries.Add(new Entry(row, column, value));
    }

    /// <summary>
    /// 追加一个对称条目；当行列索引不同时，同时追加其转置位置。
    /// </summary>
    /// <param name="row">条目行索引。</param>
    /// <param name="column">条目列索引。</param>
    /// <param name="value">条目值。</param>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    public void AddSymmetric(int row, int column, double value)
    {
        this.Add(row, column, value);

        if (row != column)
        {
            this.Add(column, row, value);
        }
    }

    /// <summary>
    /// 追加一个使用同一索引集合作为行和列索引的局部矩阵。
    /// </summary>
    /// <param name="indices">局部矩阵对应的全局行列索引。</param>
    /// <param name="values">按行优先顺序排列的局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于索引数量的平方时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">索引超出矩阵范围时抛出。</exception>
    public void AddSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
        => this.AddSubmatrix(indices, indices, values);

    /// <summary>
    /// 追加一个局部矩阵。
    /// </summary>
    /// <param name="rowIndices">局部矩阵的全局行索引。</param>
    /// <param name="columnIndices">局部矩阵的全局列索引。</param>
    /// <param name="values">按行优先顺序排列的局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于行索引数量与列索引数量的乘积时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">索引超出矩阵范围时抛出。</exception>
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

    /// <summary>
    /// 追加一个对称局部矩阵，只读取并展开其上三角部分。
    /// </summary>
    /// <param name="indices">局部矩阵对应的全局行列索引。</param>
    /// <param name="values">按行优先顺序排列的完整对称局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于索引数量的平方时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">索引超出矩阵范围时抛出。</exception>
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

    /// <summary>
    /// 将已追加的条目合并并转换为 CSR 矩阵。
    /// </summary>
    /// <returns>CSR 格式的矩阵。相同位置的重复条目会求和，合并后为零的条目不会存储。</returns>
    public CsrMatrix ToCsr()
    {
        if (this._entries.Count == 0)
        {
            return new CsrMatrix(this.RowCount, this.ColumnCount, new int[this.RowCount + 1], [], []);
        }

        var entries = this._entries.ToArray();
        Array.Sort(entries);

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

    /// <summary>
    /// 将合并后的矩阵条目追加到 CSR 转换所需的中间缓冲区。
    /// </summary>
    /// <param name="row">条目行索引。</param>
    /// <param name="column">条目列索引。</param>
    /// <param name="value">合并后的条目值。</param>
    /// <param name="rowCounts">记录每一行已追加条目数量的数组。</param>
    /// <param name="columns">存储条目列索引的列表。</param>
    /// <param name="values">存储条目值的列表。</param>
    /// <remarks>值为零的条目不会被追加。</remarks>
    private static void AddMergedEntry(int row, int column, double value,
        int[] rowCounts, List<int> columns, List<double> values)
    {
        if (value == 0.0) return;

        rowCounts[row]++;
        columns.Add(column);
        values.Add(value);
    }

    private readonly record struct Entry(int Row, int Column, double Value) : IComparable<Entry>
    {
        public int CompareTo(Entry other)
            => (this.Row, this.Column).CompareTo((other.Row, other.Column));
    }
}
