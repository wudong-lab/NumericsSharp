using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Solvers.LinearSolvers;

public interface ILinearSolver
{
    SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution);
}
