using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Native;

internal sealed partial class PardisoNativeHandle : SafeHandle
{
    private PardisoNativeHandle() : base(nint.Zero, ownsHandle: true) { }

    private PardisoNativeHandle(nint handle) : base(nint.Zero, ownsHandle: true)
    {
        this.SetHandle(handle);
    }

    public override bool IsInvalid => this.handle == nint.Zero;

    internal static PardisoNativeHandle Create()
    {
        MklBackendException.ThrowIfFailed(NumericsSharp_PardisoCreate(out var handle));
        return new PardisoNativeHandle(handle);
    }

    internal static MklNativeStatus SetThreadCount(int threadCount)
        => NumericsSharp_MklSetThreadCount(threadCount);

    internal unsafe MklNativeStatus GetLastError(int* phase, int* error)
        => NumericsSharp_PardisoGetLastError(this, phase, error);

    internal unsafe MklNativeStatus Analyze(
        int order,
        int nonZeroCount,
        int* rowPointers,
        int* columns,
        PardisoMatrixType matrixType)
        => NumericsSharp_PardisoAnalyze(this, order, nonZeroCount, rowPointers, columns, matrixType);

    internal unsafe MklNativeStatus Factorize(double* values)
        => NumericsSharp_PardisoFactorize(this, values);

    internal unsafe MklNativeStatus Solve(
        double* rightHandSide,
        double* solution,
        int rightHandSideCount)
        => NumericsSharp_PardisoSolve(this, rightHandSide, solution, rightHandSideCount);

    protected override bool ReleaseHandle()
    {
        return NumericsSharp_PardisoDestroy(this.handle) == MklNativeStatus.Success;
    }

    #region Interop

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoCreate")]
    private static partial MklNativeStatus NumericsSharp_PardisoCreate(out nint handle);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoDestroy")]
    private static partial MklNativeStatus NumericsSharp_PardisoDestroy(nint handle);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_MklSetThreadCount")]
    private static partial MklNativeStatus NumericsSharp_MklSetThreadCount(int threadCount);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoGetLastError")]
    private static unsafe partial MklNativeStatus NumericsSharp_PardisoGetLastError(
        PardisoNativeHandle handle,
        int* phase,
        int* error);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoAnalyze")]
    private static unsafe partial MklNativeStatus NumericsSharp_PardisoAnalyze(
        PardisoNativeHandle handle,
        int order,
        int nonZeroCount,
        int* rowPointers,
        int* columns,
        PardisoMatrixType matrixType);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoFactorize")]
    private static unsafe partial MklNativeStatus NumericsSharp_PardisoFactorize(
        PardisoNativeHandle handle,
        double* values);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoSolve")]
    private static unsafe partial MklNativeStatus NumericsSharp_PardisoSolve(
        PardisoNativeHandle handle,
        double* rightHandSide,
        double* solution,
        int rightHandSideCount);

    #endregion
}
