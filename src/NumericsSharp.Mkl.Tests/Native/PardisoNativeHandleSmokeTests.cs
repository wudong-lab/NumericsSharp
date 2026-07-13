using NumericsSharp.Mkl.Native;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed unsafe class PardisoNativeHandleSmokeTests
{
    [Fact]
    public void CreateDestroyAndAnalyze_CanCallNativeDllWhenBuilt()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        using var handle = PardisoNativeHandle.Create();
        var rowPointers = stackalloc[] { 1, 2, 3 };
        var columns = stackalloc[] { 1, 2 };

        var analyzeStatus = handle.Analyze(
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

        using var handle = PardisoNativeHandle.Create();
        int phase;
        int error;
        var status = handle.GetLastError(&phase, &error);

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

        var status = PardisoNativeHandle.SetThreadCount(0);

        Assert.Equal(MklNativeStatus.InvalidArgument, status);
    }
}
