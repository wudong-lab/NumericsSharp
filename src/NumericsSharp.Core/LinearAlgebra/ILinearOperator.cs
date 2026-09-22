namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 表示可执行矩阵-向量乘法的线性算子。
/// </summary>
public interface ILinearOperator
{
    /// <summary>
    /// 获取算子的行数。
    /// </summary>
    int RowCount { get; }

    /// <summary>
    /// 获取算子的列数。
    /// </summary>
    int ColumnCount { get; }

    /// <summary>
    /// 计算算子与向量的乘积 <c>y = A * x</c>。
    /// </summary>
    /// <param name="x">输入向量，长度必须等于 <see cref="ColumnCount"/>。</param>
    /// <param name="y">用于接收结果的输出向量，长度必须等于 <see cref="RowCount"/>。</param>
    void Multiply(ReadOnlySpan<double> x, Span<double> y);
}
