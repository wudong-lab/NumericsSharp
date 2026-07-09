using NumericsSharp.Mkl;
using NumericsSharp.Mkl.Native;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed class MklNativeExceptionTests
{
    [Fact]
    public void ThrowIfFailed_DoesNotThrowForSuccess()
    {
        MklNativeException.ThrowIfFailed(MklNativeStatus.Success);
    }

    [Fact]
    public void ThrowIfFailed_ThrowsForFailureStatus()
    {
        var exception = Assert.Throws<MklBackendException>(
            () => MklNativeException.ThrowIfFailed(MklNativeStatus.InvalidArgument));

        Assert.Equal((int)MklNativeStatus.InvalidArgument, exception.StatusCode);
        Assert.Equal(nameof(MklNativeStatus.InvalidArgument), exception.StatusName);
    }
}
