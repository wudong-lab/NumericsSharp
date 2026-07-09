using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.LinearSolvers;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.ConjugateGradient;

public sealed class ConjugateGradientLinearSolver : ILinearSolver
{
    private readonly ConjugateGradientSolver _conjugateGradientSolver = new();
    private readonly PreconditionedConjugateGradientSolver _preconditionedConjugateGradientSolver = new();

    public ConjugateGradientLinearSolver(
        ConjugateGradientOptions? options = null,
        IPreconditioner? preconditioner = null)
    {
        Options = options ?? new ConjugateGradientOptions();
        Preconditioner = preconditioner;
    }

    public ConjugateGradientOptions Options { get; }

    public IPreconditioner? Preconditioner { get; }

    public SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        if (Preconditioner is null)
        {
            return _conjugateGradientSolver.Solve(matrix, rightHandSide, solution, Options);
        }

        return _preconditionedConjugateGradientSolver.Solve(matrix, Preconditioner, rightHandSide, solution, Options);
    }
}
