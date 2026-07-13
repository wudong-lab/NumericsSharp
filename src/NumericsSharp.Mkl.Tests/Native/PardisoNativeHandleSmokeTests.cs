using NumericsSharp.Mkl.Interop;
using NumericsSharp.Mkl.Pardiso;
using PardisoNativeHandle = NumericsSharp.Mkl.Pardiso.PardisoNativeHandle;

namespace NumericsSharp.Mkl.Tests.Native;

public sealed class PardisoNativeHandleSmokeTests
{
    [Fact]
    public void CreateDestroyAndAnalyze_CanCallNativeDllWhenBuilt()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        using var handle = PardisoNativeHandle.Create();
        Span<int> rowPointers = stackalloc[] { 1, 2, 3 };
        Span<int> columns = stackalloc[] { 1, 2 };

        var analyzeStatus = handle.Analyze(
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
        var status = handle.GetLastError(out phase, out error);

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
