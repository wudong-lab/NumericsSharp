namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 表示 CSR 矩阵的稀疏结构，不包含条目数值。
/// </summary>
public sealed class CsrMatrixPattern
{
    private CsrMatrixPattern(int rowCount, int columnCount, int[] rowOffsets, int[] columnIndices)
    {
        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this.RowOffsets = rowOffsets;
        this.ColumnIndices = columnIndices;
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
    /// 获取结构中包含的条目数。
    /// </summary>
    public int NonZeroCount => this.ColumnIndices.Length;

    /// <summary>
    /// 获取 CSR 行偏移数组。
    /// </summary>
    /// <remarks>
    /// 数组由当前结构直接持有，并可能与多个矩阵或组装器共享；调用方不得在结构使用期间修改其内容。
    /// 如需独立的结构副本，请使用 <see cref="Clone"/>。
    /// </remarks>
    public int[] RowOffsets { get; }

    /// <summary>
    /// 获取 CSR 列索引数组。
    /// </summary>
    /// <remarks>
    /// 数组由当前结构直接持有，并可能与多个矩阵或组装器共享；调用方不得在结构使用期间修改其内容。
    /// 如需独立的结构副本，请使用 <see cref="Clone"/>。
    /// </remarks>
    public int[] ColumnIndices { get; }

    /// <summary>
    /// 创建当前 CSR 结构的独立深复制。
    /// </summary>
    /// <returns>不与当前实例共享结构数组的新 CSR 结构。</returns>
    public CsrMatrixPattern Clone()
        => new(
            this.RowCount,
            this.ColumnCount,
            (int[])this.RowOffsets.Clone(),
            (int[])this.ColumnIndices.Clone());

    internal static CsrMatrixPattern Create(
        int rowCount,
        int columnCount,
        int[] rowOffsets,
        int[] columnIndices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
        ArgumentNullException.ThrowIfNull(rowOffsets);
        ArgumentNullException.ThrowIfNull(columnIndices);

        if (rowOffsets.Length != rowCount + 1)
            throw new ArgumentException("CSR row offset count must equal rowCount + 1.", nameof(rowOffsets));

        if (rowOffsets[0] != 0 || rowOffsets[^1] != columnIndices.Length)
            throw new ArgumentException("CSR row offsets are inconsistent with column index count.", nameof(rowOffsets));

        for (var row = 0; row < rowCount; row++)
        {
            var start = rowOffsets[row];
            var end = rowOffsets[row + 1];

            if (start > end)
                throw new ArgumentException("CSR row offsets must be nondecreasing.", nameof(rowOffsets));

            var previousColumn = -1;
            for (var index = start; index < end; index++)
            {
                var column = columnIndices[index];
                if ((uint)column >= (uint)columnCount)
                    throw new ArgumentOutOfRangeException(nameof(columnIndices), "CSR column index is out of range.");

                if (column <= previousColumn)
                {
                    throw new ArgumentException(
                        "CSR column indices must be strictly increasing within each row.",
                        nameof(columnIndices));
                }

                previousColumn = column;
            }
        }

        return new CsrMatrixPattern(rowCount, columnCount, rowOffsets, columnIndices);
    }

    /// <summary>
    /// 创建与当前结构匹配的数值缓冲区。
    /// </summary>
    /// <returns>长度为 <see cref="NonZeroCount"/> 且所有元素为零的数组。</returns>
    public double[] CreateValueBuffer() => new double[this.NonZeroCount];

    /// <summary>
    /// 查找指定矩阵位置在 CSR 数组中的条目索引。
    /// </summary>
    /// <param name="row">矩阵行索引。</param>
    /// <param name="column">矩阵列索引。</param>
    /// <returns>指定位置对应的 CSR 条目索引。</returns>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    /// <exception cref="ArgumentException">结构中不存在指定位置的条目时抛出。</exception>
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
