using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Native;

internal static partial class PardisoNativeMethods
{
    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoCreate")]
    internal static partial MklNativeStatus Create(out nint handle);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoDestroy")]
    internal static partial MklNativeStatus Destroy(nint handle);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_MklSetThreadCount")]
    internal static partial MklNativeStatus SetThreadCount(int threadCount);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoGetLastError")]
    internal static unsafe partial MklNativeStatus GetLastError(
        PardisoNativeHandle handle,
        int* phase,
        int* error);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoAnalyze")]
    internal static unsafe partial MklNativeStatus Analyze(
        PardisoNativeHandle handle,
        int order,
        int nonZeroCount,
        int* rowPointers,
        int* columns,
        PardisoMatrixType matrixType);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoFactorize")]
    internal static unsafe partial MklNativeStatus Factorize(
        PardisoNativeHandle handle,
        double* values);

    [LibraryImport(InteropInfo.LibraryName, EntryPoint = "NumericsSharp_PardisoSolve")]
    internal static unsafe partial MklNativeStatus Solve(
        PardisoNativeHandle handle,
        double* rightHandSide,
        double* solution,
        int rightHandSideCount);
}