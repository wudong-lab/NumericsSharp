using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Native;

internal static partial class PardisoNativeMethods
{
    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoCreate")]
    internal static partial MklNativeStatus Create(out IntPtr handle);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoDestroy")]
    internal static partial MklNativeStatus Destroy(IntPtr handle);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_MklSetThreadCount")]
    internal static partial MklNativeStatus SetThreadCount(int threadCount);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoGetLastError")]
    internal static unsafe partial MklNativeStatus GetLastError(
        PardisoNativeHandle handle,
        int* phase,
        int* error);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoAnalyze")]
    internal static unsafe partial MklNativeStatus Analyze(
        PardisoNativeHandle handle,
        int order,
        int nonZeroCount,
        int* rowPointers,
        int* columns,
        PardisoMatrixType matrixType);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoFactorize")]
    internal static unsafe partial MklNativeStatus Factorize(
        PardisoNativeHandle handle,
        double* values);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "NumericsSharp_PardisoSolve")]
    internal static unsafe partial MklNativeStatus Solve(
        PardisoNativeHandle handle,
        double* rightHandSide,
        double* solution,
        int rightHandSideCount);
}
