namespace NumericsSharp.Solvers.ConjugateGradient;

public sealed record ConjugateGradientOptions
{
    public int MaxIterations { get; init; } = 1_000;

    public double RelativeTolerance { get; init; } = 1e-10;
}
