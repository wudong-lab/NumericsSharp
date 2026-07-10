namespace NumericsSharp.Solvers.LinearSolvers;

public sealed record SolverResult(SolverStatus Status, int IterationCount, double InitialResidualNorm, double FinalResidualNorm)
{
    public bool Converged => this.Status == SolverStatus.Converged;
}
