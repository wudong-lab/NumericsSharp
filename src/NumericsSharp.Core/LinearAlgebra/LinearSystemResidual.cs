namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 提供线性方程组残差计算功能。
/// </summary>
public static class LinearSystemResidual
{
    /// <summary>
    /// 计算线性方程组 <c>A * x = b</c> 的二范数残差 <c>||A * x - b||₂</c>。
    /// </summary>
    /// <param name="matrix">用于计算 <c>A * x</c> 的线性算子。</param>
    /// <param name="solution">待评估的解向量 <c>x</c>。</param>
    /// <param name="rightHandSide">方程组右端项 <c>b</c>。</param>
    /// <returns>残差向量的欧几里得范数。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">解向量或右端项长度与算子维度不匹配时抛出。</exception>
    public static double ComputeL2Norm(ILinearOperator matrix, ReadOnlySpan<double> solution, ReadOnlySpan<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (solution.Length != matrix.ColumnCount)
            throw new ArgumentException("Solution length must equal matrix column count.", nameof(solution));

        if (rightHandSide.Length != matrix.RowCount)
            throw new ArgumentException("Right hand side length must equal matrix row count.", nameof(rightHandSide));

        var matrixTimesSolution = new double[matrix.RowCount];
        matrix.Multiply(solution, matrixTimesSolution);

        var squaredNorm = 0.0;
        for (var i = 0; i < matrixTimesSolution.Length; i++)
        {
            var residual = matrixTimesSolution[i] - rightHandSide[i];
            squaredNorm += residual * residual;
        }

        return Math.Sqrt(squaredNorm);
    }
}
