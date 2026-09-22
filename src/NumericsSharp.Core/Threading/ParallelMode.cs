namespace NumericsSharp.Core.Threading;

/// <summary>
/// 表示并行计算的线程控制模式。
/// </summary>
public enum ParallelMode
{
    /// <summary>
    /// 使用底层原生库的内部并行，外层托管代码不进行并行调度。
    /// </summary>
    NativeInnerParallel,

    /// <summary>
    /// 使用托管代码进行外层并行，底层原生库使用单线程或受控线程数运行。
    /// </summary>
    ManagedOuterParallel,
}
