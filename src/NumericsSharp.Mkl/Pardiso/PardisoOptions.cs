using NumericsSharp.Core.Threading;

namespace NumericsSharp.Mkl.Pardiso;

public sealed record PardisoOptions(PardisoMatrixType MatrixType)
{
    public NumericsThreadingOptions Threading { get; init; } = new();
}
