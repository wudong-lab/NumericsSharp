namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 基于固定 CSR 结构累加矩阵数值，适合重复组装具有相同稀疏模式的矩阵。
/// </summary>
public sealed class SparseMatrixAssembler
{
    private readonly double[] _values;

    /// <summary>
    /// 创建稀疏矩阵组装器。
    /// </summary>
    /// <param name="pattern">要填充的 CSR 矩阵结构。</param>
    /// <remarks>组装器只修改自己的数值缓冲区，与 <paramref name="pattern"/> 共享结构数组；构造后不得修改该结构。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> 为 <see langword="null"/> 时抛出。</exception>
    public SparseMatrixAssembler(CsrMatrixPattern pattern)
    {
        this.Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this._values = pattern.CreateValueBuffer();
    }

    /// <summary>
    /// 获取组装器使用的 CSR 矩阵结构。
    /// </summary>
    public CsrMatrixPattern Pattern { get; }

    /// <summary>
    /// 清除当前已累加的所有数值，但保留矩阵结构。
    /// </summary>
    public void Clear() => Array.Clear(this._values);

    /// <summary>
    /// 将一个矩阵条目累加到固定结构中。数值为零的条目会被忽略。
    /// </summary>
    /// <param name="row">条目行索引。</param>
    /// <param name="column">条目列索引。</param>
    /// <param name="value">要累加的条目值。</param>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    /// <exception cref="ArgumentException">固定结构中不存在指定位置的条目时抛出。</exception>
    public void Add(int row, int column, double value)
    {
        if (value == 0.0) return;

        var entryIndex = this.Pattern.FindEntryIndex(row, column);
        this._values[entryIndex] += value;
    }

    /// <summary>
    /// 将一个对称条目累加到固定结构中；当行列索引不同时，同时累加其转置位置。
    /// </summary>
    /// <param name="row">条目行索引。</param>
    /// <param name="column">条目列索引。</param>
    /// <param name="value">要累加的条目值。</param>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    /// <exception cref="ArgumentException">固定结构中不存在指定位置的条目时抛出。</exception>
    public void AddSymmetric(int row, int column, double value)
    {
        this.Add(row, column, value);

        if (row != column)
        {
            this.Add(column, row, value);
        }
    }

    /// <summary>
    /// 将一个使用同一索引集合作为行和列索引的局部矩阵累加到固定结构中。
    /// </summary>
    /// <param name="indices">局部矩阵对应的全局行列索引。</param>
    /// <param name="values">按行优先顺序排列的局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于索引数量的平方，或固定结构缺少目标条目时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">索引超出矩阵范围时抛出。</exception>
    public void AddSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
        => this.AddSubmatrix(indices, indices, values);

    /// <summary>
    /// 将一个局部矩阵累加到固定结构中。
    /// </summary>
    /// <param name="rowIndices">局部矩阵的全局行索引。</param>
    /// <param name="columnIndices">局部矩阵的全局列索引。</param>
    /// <param name="values">按行优先顺序排列的局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于行索引数量与列索引数量的乘积，或固定结构缺少目标条目时抛出。</exception>
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
    /// 将一个对称局部矩阵累加到固定结构中，只读取并展开其上三角部分。
    /// </summary>
    /// <param name="indices">局部矩阵对应的全局行列索引。</param>
    /// <param name="values">按行优先顺序排列的完整对称局部矩阵值。</param>
    /// <exception cref="ArgumentException">值数量不等于索引数量的平方，或固定结构缺少目标条目时抛出。</exception>
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
    /// 根据当前累加结果创建 CSR 矩阵。
    /// </summary>
    /// <returns>包含当前结构和累加数值的新 CSR 矩阵。</returns>
    public CsrMatrix ToCsr() => new(this.Pattern, (double[])this._values.Clone());
}
