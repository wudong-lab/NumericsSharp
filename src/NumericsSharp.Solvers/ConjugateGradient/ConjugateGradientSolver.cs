using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Solvers.ConjugateGradient;

public sealed class ConjugateGradientSolver : ILinearSolver
{
    public SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
        => Solve(matrix, rightHandSide, solution, options: null);

    public SolverResult Solve(
        ILinearOperator matrix,
        ReadOnlySpan<double> rightHandSide,
        Span<double> solution,
        ConjugateGradientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("Conjugate Gradient requires a square matrix.", nameof(matrix));
        }

        if (rightHandSide.Length != matrix.RowCount)
        {
            throw new ArgumentException("Right hand side length must equal matrix row count.", nameof(rightHandSide));
        }

        if (solution.Length != matrix.ColumnCount)
        {
            throw new ArgumentException("Solution length must equal matrix column count.", nameof(solution));
        }

        options ??= new ConjugateGradientOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxIterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.RelativeTolerance);

        var n = matrix.RowCount;
        var residual = new double[n];
        var direction = new double[n];
        var matrixDirection = new double[n];

        matrix.Multiply(solution, matrixDirection);

        for (var i = 0; i < n; i++)
        {
            residual[i] = rightHandSide[i] - matrixDirection[i];
            direction[i] = residual[i];
        }

        var residualSquared = Dot(residual, residual);
        var initialResidualNorm = Math.Sqrt(residualSquared);
        var tolerance = Math.Max(1.0, initialResidualNorm) * options.RelativeTolerance;

        if (initialResidualNorm <= tolerance)
        {
            return new SolverResult(SolverStatus.Converged, 0, initialResidualNorm, initialResidualNorm);
        }

        for (var iteration = 1; iteration <= options.MaxIterations; iteration++)
        {
            matrix.Multiply(direction, matrixDirection);

            var denominator = Dot(direction, matrixDirection);
            if (denominator <= 0.0 || !double.IsFinite(denominator))
            {
                return new SolverResult(SolverStatus.Breakdown, iteration - 1, initialResidualNorm, Math.Sqrt(residualSquared));
            }

            var alpha = residualSquared / denominator;

            for (var i = 0; i < n; i++)
            {
                solution[i] += alpha * direction[i];
                residual[i] -= alpha * matrixDirection[i];
            }

            var nextResidualSquared = Dot(residual, residual);
            var residualNorm = Math.Sqrt(nextResidualSquared);

            if (residualNorm <= tolerance)
            {
                return new SolverResult(SolverStatus.Converged, iteration, initialResidualNorm, residualNorm);
            }

            var beta = nextResidualSquared / residualSquared;
            for (var i = 0; i < n; i++)
            {
                direction[i] = residual[i] + beta * direction[i];
            }

            residualSquared = nextResidualSquared;
        }

        return new SolverResult(SolverStatus.MaxIterationsReached, options.MaxIterations, initialResidualNorm, Math.Sqrt(residualSquared));
    }

    private static double Dot(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        var sum = 0.0;

        for (var i = 0; i < x.Length; i++)
        {
            sum += x[i] * y[i];
        }

        return sum;
    }
}
