namespace NumericsSharp.Core.Threading;

/// <summary>
/// 配置数值计算中的托管线程和底层原生线程使用方式。
/// </summary>
public sealed record NumericsThreadingOptions
{
    /// <summary>
    /// 获取或设置并行计算模式。默认使用底层原生库内部并行。
    /// </summary>
    public ParallelMode Mode { get; init; } = ParallelMode.NativeInnerParallel;

    /// <summary>
    /// 获取或设置托管外层并行的最大并行度。默认值为 1。
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;

    /// <summary>
    /// 获取或设置底层原生库使用的线程数。默认值为当前计算机的处理器数。
    /// </summary>
    public int NativeThreadCount { get; init; } = Environment.ProcessorCount;
}
