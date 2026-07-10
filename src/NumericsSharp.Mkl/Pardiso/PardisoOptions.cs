using NumericsSharp.Core.Threading;

namespace NumericsSharp.Mkl.Pardiso;

public sealed record PardisoOptions
{
    public PardisoMatrixType MatrixType { get; init; } = PardisoMatrixType.RealSymmetricPositiveDefinite;
    public NumericsThreadingOptions Threading { get; init; } = new();
}