using NumericsSharp.Mkl.Native;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed unsafe class PardisoNativeMethodsSmokeTests
{
    [Fact]
    public void CreateDestroyAndAnalyze_CanCallNativeDllWhenBuilt()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var createStatus = PardisoNativeMethods.Create(out var rawHandle);
        Assert.Equal(MklNativeStatus.Success, createStatus);
        Assert.NotEqual(IntPtr.Zero, rawHandle);

        using var handle = new PardisoNativeHandle(rawHandle, ownsHandle: true);
        var rowPointers = stackalloc[] { 1, 2, 3 };
        var columns = stackalloc[] { 1, 2 };

        var analyzeStatus = PardisoNativeMethods.Analyze(
            handle,
            order: 2,
            nonZeroCount: 2,
            rowPointers,
            columns,
            PardisoMatrixType.RealSymmetricPositiveDefinite);

        Assert.Equal(MklNativeStatus.Success, analyzeStatus);
    }

    [Fact]
    public void GetLastError_ReturnsInitialPardisoErrorState()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var createStatus = PardisoNativeMethods.Create(out var rawHandle);
        Assert.Equal(MklNativeStatus.Success, createStatus);

        using var handle = new PardisoNativeHandle(rawHandle, ownsHandle: true);
        int phase;
        int error;
        var status = PardisoNativeMethods.GetLastError(handle, &phase, &error);

        Assert.Equal(MklNativeStatus.Success, status);
        Assert.Equal(0, phase);
        Assert.Equal(0, error);
    }

    [Fact]
    public void SetThreadCount_RejectsNonPositiveThreadCount()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var status = PardisoNativeMethods.SetThreadCount(0);

        Assert.Equal(MklNativeStatus.InvalidArgument, status);
    }
}
