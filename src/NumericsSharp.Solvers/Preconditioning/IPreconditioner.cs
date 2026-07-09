namespace NumericsSharp.Solvers.Preconditioning;

public interface IPreconditioner
{
    int Order { get; }

    void Apply(ReadOnlySpan<double> residual, Span<double> result);
}
