using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.LinearSolvers;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.ConjugateGradient;

public sealed class PreconditionedConjugateGradientSolver
{
    public SolverResult Solve(
        ILinearOperator matrix,
        IPreconditioner preconditioner,
        ReadOnlySpan<double> rightHandSide,
        Span<double> solution,
        ConjugateGradientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(preconditioner);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("Preconditioned Conjugate Gradient requires a square matrix.", nameof(matrix));
        }

        if (preconditioner.Order != matrix.RowCount)
        {
            throw new ArgumentException("Preconditioner order must equal matrix order.", nameof(preconditioner));
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
        var preconditionedResidual = new double[n];
        var direction = new double[n];
        var matrixDirection = new double[n];

        matrix.Multiply(solution, matrixDirection);

        for (var i = 0; i < n; i++)
        {
            residual[i] = rightHandSide[i] - matrixDirection[i];
        }

        var residualSquared = Dot(residual, residual);
        var initialResidualNorm = Math.Sqrt(residualSquared);
        var tolerance = Math.Max(1.0, initialResidualNorm) * options.RelativeTolerance;

        if (initialResidualNorm <= tolerance)
        {
            return new SolverResult(SolverStatus.Converged, 0, initialResidualNorm, initialResidualNorm);
        }

        preconditioner.Apply(residual, preconditionedResidual);
        Array.Copy(preconditionedResidual, direction, n);

        var residualDotPreconditioned = Dot(residual, preconditionedResidual);
        if (residualDotPreconditioned <= 0.0 || !double.IsFinite(residualDotPreconditioned))
        {
            return new SolverResult(SolverStatus.Breakdown, 0, initialResidualNorm, initialResidualNorm);
        }

        for (var iteration = 1; iteration <= options.MaxIterations; iteration++)
        {
            matrix.Multiply(direction, matrixDirection);

            var denominator = Dot(direction, matrixDirection);
            if (denominator <= 0.0 || !double.IsFinite(denominator))
            {
                return new SolverResult(SolverStatus.Breakdown, iteration - 1, initialResidualNorm, Math.Sqrt(residualSquared));
            }

            var alpha = residualDotPreconditioned / denominator;

            for (var i = 0; i < n; i++)
            {
                solution[i] += alpha * direction[i];
                residual[i] -= alpha * matrixDirection[i];
            }

            residualSquared = Dot(residual, residual);
            var residualNorm = Math.Sqrt(residualSquared);
            if (residualNorm <= tolerance)
            {
                return new SolverResult(SolverStatus.Converged, iteration, initialResidualNorm, residualNorm);
            }

            preconditioner.Apply(residual, preconditionedResidual);

            var nextResidualDotPreconditioned = Dot(residual, preconditionedResidual);
            if (nextResidualDotPreconditioned <= 0.0 || !double.IsFinite(nextResidualDotPreconditioned))
            {
                return new SolverResult(SolverStatus.Breakdown, iteration, initialResidualNorm, residualNorm);
            }

            var beta = nextResidualDotPreconditioned / residualDotPreconditioned;
            for (var i = 0; i < n; i++)
            {
                direction[i] = preconditionedResidual[i] + beta * direction[i];
            }

            residualDotPreconditioned = nextResidualDotPreconditioned;
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
