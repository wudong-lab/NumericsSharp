using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Pardiso;

namespace NumericsSharp.Mkl.Native;

internal static partial class PardisoNativeMethods
{
    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "ns_pardiso_create")]
    internal static partial MklNativeStatus Create(out IntPtr handle);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "ns_pardiso_destroy")]
    internal static partial MklNativeStatus Destroy(IntPtr handle);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "ns_pardiso_analyze")]
    internal static unsafe partial MklNativeStatus Analyze(
        PardisoNativeHandle handle,
        int order,
        int nonZeroCount,
        int* rowPointers,
        int* columns,
        PardisoMatrixType matrixType);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "ns_pardiso_factorize")]
    internal static unsafe partial MklNativeStatus Factorize(
        PardisoNativeHandle handle,
        double* values);

    [LibraryImport(MklNativeConstants.LibraryName, EntryPoint = "ns_pardiso_solve")]
    internal static unsafe partial MklNativeStatus Solve(
        PardisoNativeHandle handle,
        double* rightHandSide,
        double* solution,
        int rightHandSideCount);
}
