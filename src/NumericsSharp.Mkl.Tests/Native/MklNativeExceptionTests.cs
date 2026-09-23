using NumericsSharp.Mkl.Interop;

namespace NumericsSharp.Mkl.Tests.Native;

[TestClass]
public sealed class MklNativeExceptionTests
{
    [TestMethod]
    public void ThrowIfFailed_DoesNotThrowForSuccess()
    {
        MklBackendException.ThrowIfFailed(MklNativeStatus.Success);
    }

    [TestMethod]
    public void ThrowIfFailed_ThrowsForFailureStatus()
    {
        var exception = Assert.ThrowsExactly<MklBackendException>(
            () => MklBackendException.ThrowIfFailed(MklNativeStatus.InvalidArgument));

        Assert.AreEqual((int)MklNativeStatus.InvalidArgument, exception.StatusCode);
        Assert.AreEqual(nameof(MklNativeStatus.InvalidArgument), exception.StatusName);
    }

    [TestMethod]
    public void ThrowIfFailed_IncludesPardisoContext()
    {
        var exception = Assert.ThrowsExactly<MklBackendException>(
            () => MklBackendException.ThrowIfFailed(
                MklNativeStatus.MklError,
                operation: "PARDISO factorize",
                phase: 12,
                matrixType: "RealSymmetricPositiveDefinite",
                order: 3,
                nonZeroCount: 6,
                pardisoErrorCode: -4));

        Assert.AreEqual("PARDISO factorize", exception.Operation);
        Assert.AreEqual(12, exception.Phase);
        Assert.AreEqual("RealSymmetricPositiveDefinite", exception.MatrixType);
        Assert.AreEqual(3, exception.Order);
        Assert.AreEqual(6, exception.NonZeroCount);
        Assert.AreEqual(-4, exception.PardisoErrorCode);
        StringAssert.Contains(exception.Message, "PARDISO error code: -4");
    }
}
