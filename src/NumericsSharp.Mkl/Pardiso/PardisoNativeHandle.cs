using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Interop;

namespace NumericsSharp.Mkl.Pardiso;

internal sealed partial class PardisoNativeHandle : SafeHandle
{
    private PardisoNativeHandle(nint handle) : base(nint.Zero, ownsHandle: true)
    {
        this.SetHandle(handle);
    }

    public override bool IsInvalid => this.handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        return NumericsSharp_PardisoDestroy(this.handle) == MklNativeStatus.Success;
    }

    internal static PardisoNativeHandle Create()
    {
        MklBackendException.ThrowIfFailed(NumericsSharp_PardisoCreate(out var handle));
        return new PardisoNativeHandle(handle);
    }

    internal static MklNativeStatus SetThreadCount(int threadCount)
        => NumericsSharp_MklSetThreadCount(threadCount);

    internal unsafe MklNativeStatus GetLastError(out int phase, out int error)
    {
        int nativePhase;
        int nativeError;
        var status = NumericsSharp_PardisoGetLastError(this, &nativePhase, &nativeError);
        phase = nativePhase;
        error = nativeError;
        return status;
    }

    internal unsafe MklNativeStatus Analyze(ReadOnlySpan<int> rowPointers, ReadOnlySpan<int> columns, PardisoMatrixType matrixType)
    {
        fixed (int* rowPointersPointer = rowPointers)
        fixed (int* columnsPointer = columns)
        {
            return NumericsSharp_PardisoAnalyze(
                this,
                rowPointers.Length - 1,
                columns.Length,
                rowPointersPointer,
                columnsPointer,
                matrixType);
        }
    }

    internal unsafe MklNativeStatus Factorize(ReadOnlySpan<double> values)
    {
        fixed (double* valuesPointer = values)
        {
            return NumericsSharp_PardisoFactorize(this, valuesPointer);
        }
    }

    internal unsafe MklNativeStatus Solve(ReadOnlySpan<double> rightHandSide, Span<double> solution, int rightHandSideCount)
    {
        fixed (double* rightHandSidePointer = rightHandSide)
        fixed (double* solutionPointer = solution)
        {
            return NumericsSharp_PardisoSolve(
                this,
                rightHandSidePointer,
                solutionPointer,
                rightHandSideCount);
        }
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