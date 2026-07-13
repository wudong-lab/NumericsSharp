using NumericsSharp.Mkl;
using NumericsSharp.Mkl.Native;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed class MklNativeExceptionTests
{
    [Fact]
    public void ThrowIfFailed_DoesNotThrowForSuccess()
    {
        MklBackendException.ThrowIfFailed(MklNativeStatus.Success);
    }

    [Fact]
    public void ThrowIfFailed_ThrowsForFailureStatus()
    {
        var exception = Assert.Throws<MklBackendException>(
            () => MklBackendException.ThrowIfFailed(MklNativeStatus.InvalidArgument));

        Assert.Equal((int)MklNativeStatus.InvalidArgument, exception.StatusCode);
        Assert.Equal(nameof(MklNativeStatus.InvalidArgument), exception.StatusName);
    }

    [Fact]
    public void ThrowIfFailed_IncludesPardisoContext()
    {
        var exception = Assert.Throws<MklBackendException>(
            () => MklBackendException.ThrowIfFailed(
                MklNativeStatus.MklError,
                operation: "PARDISO factorize",
                phase: 12,
                matrixType: "RealSymmetricPositiveDefinite",
                order: 3,
                nonZeroCount: 6,
                pardisoErrorCode: -4));

        Assert.Equal("PARDISO factorize", exception.Operation);
        Assert.Equal(12, exception.Phase);
        Assert.Equal("RealSymmetricPositiveDefinite", exception.MatrixType);
        Assert.Equal(3, exception.Order);
        Assert.Equal(6, exception.NonZeroCount);
        Assert.Equal(-4, exception.PardisoErrorCode);
        Assert.Contains("PARDISO error code: -4", exception.Message);
    }
}
