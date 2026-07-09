namespace NumericsSharp.Core.LinearAlgebra;

public static class LinearSystemResidual
{
    public static double ComputeL2Norm(ILinearOperator matrix, ReadOnlySpan<double> solution, ReadOnlySpan<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (solution.Length != matrix.ColumnCount)
        {
            throw new ArgumentException("Solution length must equal matrix column count.", nameof(solution));
        }

        if (rightHandSide.Length != matrix.RowCount)
        {
            throw new ArgumentException("Right hand side length must equal matrix row count.", nameof(rightHandSide));
        }

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
