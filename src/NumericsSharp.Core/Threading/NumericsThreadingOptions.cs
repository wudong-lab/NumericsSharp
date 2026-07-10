namespace NumericsSharp.Core.Threading;

public sealed record NumericsThreadingOptions
{
    public ParallelMode Mode { get; init; } = ParallelMode.NativeInnerParallel;
    public int MaxDegreeOfParallelism { get; init; } = 1;
    public int NativeThreadCount { get; init; } = Environment.ProcessorCount;
}
