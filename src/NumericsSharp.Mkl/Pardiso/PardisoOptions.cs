using NumericsSharp.Core.Threading;

namespace NumericsSharp.Mkl.Pardiso;

public sealed record PardisoOptions(PardisoMatrixType MatrixType)
{
    public NumericsThreadingOptions Threading { get; init; } = new();

    /// <summary>
    /// 获取或设置是否在求解前后计算残差范数。
    /// </summary>
    /// <remarks>
    /// 关闭后可避免额外的 CSR 遍历，<see cref="NumericsSharp.Solvers.LinearSolvers.SolverResult"/> 中的残差值为 <see cref="double.NaN"/>。
    /// </remarks>
    public bool ComputeResiduals { get; init; } = true;
}
