using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Solvers.LinearSolvers;

public interface IDirectSparseSolver : ILinearSolver, IDisposable
{
    bool IsAnalyzed { get; }
    bool IsFactorized { get; }

    void Analyze(CsrMatrix matrix);
    void Factorize(CsrMatrix matrix);

    SolverResult Solve(CsrMatrix matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution);
    SolverResult Solve(CsrMatrix matrix, ReadOnlySpan<double> rightHandSides, Span<double> solutions, int rightHandSideCount);
}