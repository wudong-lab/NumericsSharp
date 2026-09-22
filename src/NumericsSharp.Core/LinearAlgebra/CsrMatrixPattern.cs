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
    public int[] RowOffsets { get; }

    /// <summary>
    /// 获取 CSR 列索引数组。
    /// </summary>
    public int[] ColumnIndices { get; }

    /// <summary>
    /// 从现有 CSR 矩阵复制其稀疏结构。
    /// </summary>
    /// <param name="matrix">源 CSR 矩阵。</param>
    /// <returns>与源矩阵具有相同维度和条目位置的新结构。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> 为 <see langword="null"/> 时抛出。</exception>
    public static CsrMatrixPattern FromCsr(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        return new CsrMatrixPattern(matrix.RowCount, matrix.ColumnCount,
            (int[])matrix.RowOffsets.Clone(), (int[])matrix.ColumnIndices.Clone());
    }

    /// <summary>
    /// 创建与当前结构匹配的数值缓冲区。
    /// </summary>
    /// <returns>长度为 <see cref="NonZeroCount"/> 且所有元素为零的数组。</returns>
    public double[] CreateValueBuffer() => new double[this.NonZeroCount];

    /// <summary>
    /// 使用指定数值创建 CSR 矩阵。
    /// </summary>
    /// <param name="values">按 CSR 条目顺序排列的数值。</param>
    /// <returns>由当前结构和指定数值组成的新 CSR 矩阵。</returns>
    /// <exception cref="ArgumentException">数值数量与结构中的条目数不匹配时抛出。</exception>
    public CsrMatrix ToCsr(ReadOnlySpan<double> values)
    {
        if (values.Length != this.NonZeroCount)
            throw new ArgumentException("Value count must equal pattern nonzero count.", nameof(values));

        return new CsrMatrix(this.RowCount, this.ColumnCount,
            (int[])this.RowOffsets.Clone(),
            (int[])this.ColumnIndices.Clone(),
            values.ToArray());
    }

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
