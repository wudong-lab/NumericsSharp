using NumericsSharp.Mkl.Interop;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed class NativeAbiContractTests
{
    [Fact]
    public void MklNativeStatus_ValuesMatchNativeHeader()
    {
        Assert.Equal(0, (int)MklNativeStatus.Success);
        Assert.Equal(1, (int)MklNativeStatus.InvalidArgument);
        Assert.Equal(2, (int)MklNativeStatus.MklError);
        Assert.Equal(3, (int)MklNativeStatus.OutOfMemory);
        Assert.Equal(255, (int)MklNativeStatus.UnknownError);
    }

    [Fact]
    public void PardisoMatrixType_ValuesMatchNativeHeader()
    {
        Assert.Equal(1, (int)PardisoMatrixType.RealStructurallySymmetric);
        Assert.Equal(2, (int)PardisoMatrixType.RealSymmetricPositiveDefinite);
        Assert.Equal(-2, (int)PardisoMatrixType.RealSymmetricIndefinite);
        Assert.Equal(11, (int)PardisoMatrixType.RealUnsymmetric);
    }
}
