namespace NumericsSharp.Mkl.Native;

internal static class MklNativeException
{
    public static void ThrowIfFailed(MklNativeStatus status)
        => ThrowIfFailed(
            status,
            operation: null,
            phase: null,
            matrixType: null,
            order: null,
            nonZeroCount: null,
            pardisoErrorCode: null);

    public static void ThrowIfFailed(
        MklNativeStatus status,
        string? operation,
        int? phase,
        string? matrixType,
        int? order,
        int? nonZeroCount,
        int? pardisoErrorCode)
    {
        if (status != MklNativeStatus.Success)
        {
            throw new MklBackendException(
                (int)status,
                status.ToString(),
                operation,
                phase,
                matrixType,
                order,
                nonZeroCount,
                pardisoErrorCode);
        }
    }
}
