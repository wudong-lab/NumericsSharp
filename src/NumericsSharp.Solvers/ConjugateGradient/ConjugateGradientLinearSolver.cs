using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.LinearSolvers;
using NumericsSharp.Solvers.Preconditioning;

namespace NumericsSharp.Solvers.ConjugateGradient;

public sealed class ConjugateGradientLinearSolver : ILinearSolver
{
    private readonly ConjugateGradientSolver _conjugateGradientSolver = new();
    private readonly PreconditionedConjugateGradientSolver _preconditionedConjugateGradientSolver = new();

    public ConjugateGradientLinearSolver(ConjugateGradientOptions? options = null, IPreconditioner? preconditioner = null)
    {
        this.Options = options ?? new ConjugateGradientOptions();
        this.Preconditioner = preconditioner;
    }

    public ConjugateGradientOptions Options { get; }
    public IPreconditioner? Preconditioner { get; }

    public SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        if (this.Preconditioner is null)
        {
            return this._conjugateGradientSolver.Solve(matrix, rightHandSide, solution, this.Options);
        }

        return this._preconditionedConjugateGradientSolver.Solve(matrix, this.Preconditioner, rightHandSide, solution, this.Options);
    }
}
