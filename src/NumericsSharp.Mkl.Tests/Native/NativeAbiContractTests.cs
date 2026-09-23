using NumericsSharp.Mkl.Interop;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Native;

[TestClass]
public sealed class NativeAbiContractTests
{
    [TestMethod]
    public void MklNativeStatus_ValuesMatchNativeHeader()
    {
        Assert.AreEqual(0, (int)MklNativeStatus.Success);
        Assert.AreEqual(1, (int)MklNativeStatus.InvalidArgument);
        Assert.AreEqual(2, (int)MklNativeStatus.MklError);
        Assert.AreEqual(3, (int)MklNativeStatus.OutOfMemory);
        Assert.AreEqual(255, (int)MklNativeStatus.UnknownError);
    }

    [TestMethod]
    public void PardisoMatrixType_ValuesMatchNativeHeader()
    {
        Assert.AreEqual(1, (int)PardisoMatrixType.RealStructurallySymmetric);
        Assert.AreEqual(2, (int)PardisoMatrixType.RealSymmetricPositiveDefinite);
        Assert.AreEqual(-2, (int)PardisoMatrixType.RealSymmetricIndefinite);
        Assert.AreEqual(11, (int)PardisoMatrixType.RealUnsymmetric);
    }
}
