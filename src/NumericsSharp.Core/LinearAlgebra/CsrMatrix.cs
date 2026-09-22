namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 表示采用 CSR（压缩稀疏行）格式存储的稀疏矩阵。
/// </summary>
public sealed class CsrMatrix : ILinearOperator
{
    /// <summary>
    /// 使用 CSR（压缩稀疏行）数据创建稀疏矩阵。
    /// </summary>
    /// <param name="rowCount">矩阵的行数。</param>
    /// <param name="columnCount">矩阵的列数。</param>
    /// <param name="rowOffsets">每一行在 <paramref name="columnIndices"/> 和 <paramref name="values"/> 中的起始偏移，长度必须为 <c>rowCount + 1</c>。</param>
    /// <param name="columnIndices">每个非零条目的列索引。</param>
    /// <param name="values">每个非零条目的值，与 <paramref name="columnIndices"/> 一一对应。</param>
    /// <exception cref="ArgumentOutOfRangeException">行数、列数或列索引无效时抛出。</exception>
    /// <exception cref="ArgumentException">CSR 数组的长度或行偏移不满足格式约束时抛出。</exception>
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

    /// <summary>
    /// 获取矩阵的行数。
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    /// 获取矩阵的列数。
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// 获取矩阵中存储的非零条目数。
    /// </summary>
    public int NonZeroCount => this.Values.Length;

    /// <summary>
    /// 获取 CSR 行偏移数组。
    /// </summary>
    public int[] RowOffsets { get; }

    /// <summary>
    /// 获取 CSR 列索引数组。
    /// </summary>
    public int[] ColumnIndices { get; }

    /// <summary>
    /// 获取 CSR 数值数组。
    /// </summary>
    public double[] Values { get; }

    /// <summary>
    /// 对当前方阵施加 Dirichlet 边界条件（位移边界条件），并返回约束后的新矩阵。
    /// </summary>
    /// <param name="rightHandSide">线性方程组右端项。该数组会被原地修改，以反映约束值对右端项的影响。</param>
    /// <param name="indices">受约束的自由度索引。</param>
    /// <param name="values">与 <paramref name="indices"/> 对应的约束值。</param>
    /// <returns>移除受约束行列、并在受约束对角线上设置单位值后的新 CSR 矩阵。</returns>
    /// <exception cref="ArgumentException">当前矩阵不是方阵、右端项长度不匹配或约束索引和值的数量不匹配时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">约束索引超出矩阵范围时抛出。</exception>
    public CsrMatrix ApplyDirichletBoundaryConditions(
        Span<double> rightHandSide,
        ReadOnlySpan<int> indices,
        ReadOnlySpan<double> values)
    {
        if (this.RowCount != this.ColumnCount)
            throw new ArgumentException("Dirichlet boundary conditions require a square matrix.");

        if (rightHandSide.Length != this.RowCount)
            throw new ArgumentException("Right hand side length must equal matrix order.", nameof(rightHandSide));

        if (indices.Length != values.Length)
            throw new ArgumentException("Constrained index count must equal constrained value count.", nameof(values));

        var order = this.RowCount;
        var constrainedValues = new double[order];
        var isConstrained = new bool[order];

        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            if ((uint)index >= (uint)order)
                throw new ArgumentOutOfRangeException(nameof(indices), "Constrained index is out of range.");

            if (isConstrained[index])
                throw new ArgumentException("Duplicate constrained index is not supported.", nameof(indices));

            isConstrained[index] = true;
            constrainedValues[index] = values[i];
        }

        var builder = new SparseMatrixBuilder(order, order, this.NonZeroCount + indices.Length);

        for (var row = 0; row < order; row++)
        {
            var start = this.RowOffsets[row];
            var end = this.RowOffsets[row + 1];

            for (var entryIndex = start; entryIndex < end; entryIndex++)
            {
                var column = this.ColumnIndices[entryIndex];
                var value = this.Values[entryIndex];

                if (isConstrained[column])
                {
                    rightHandSide[row] -= value * constrainedValues[column];
                }

                if (!isConstrained[row] && !isConstrained[column])
                {
                    builder.Add(row, column, value);
                }
            }
        }

        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            builder.Add(index, index, 1.0);
            rightHandSide[index] = values[i];
        }

        return builder.ToCsr();
    }

    /// <summary>
    /// 获取指定矩阵位置的值；如果该位置没有存储条目，则返回0。
    /// </summary>
    /// <param name="row">矩阵行索引。</param>
    /// <param name="column">矩阵列索引。</param>
    /// <returns>指定位置的存储值；如果该位置没有存储条目，则返回0。</returns>
    /// <exception cref="ArgumentOutOfRangeException">行索引或列索引超出矩阵范围时抛出。</exception>
    public double GetValue(int row, int column)
    {
        if ((uint)row >= (uint)this.RowCount)
            throw new ArgumentOutOfRangeException(nameof(row));

        if ((uint)column >= (uint)this.ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(column));

        for (var index = this.RowOffsets[row]; index < this.RowOffsets[row + 1]; index++)
        {
            if (this.ColumnIndices[index] == column)
            {
                return this.Values[index];
            }
        }

        return 0.0;
    }

    /// <summary>
    /// 将矩阵主对角线复制到目标缓冲区；未存储的对角线条目按零处理。
    /// </summary>
    /// <param name="diagonal">用于接收主对角线的目标缓冲区。</param>
    /// <exception cref="ArgumentException">目标缓冲区长度不等于矩阵行列数较小值时抛出。</exception>
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

    /// <summary>
    /// 计算矩阵与向量的乘积 <c>y = A * x</c>。
    /// </summary>
    /// <param name="x">输入向量，长度必须等于矩阵列数。</param>
    /// <param name="y">用于接收结果的输出向量，长度必须等于矩阵行数。</param>
    /// <exception cref="ArgumentException">输入或输出向量长度与矩阵维度不匹配时抛出。</exception>
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
