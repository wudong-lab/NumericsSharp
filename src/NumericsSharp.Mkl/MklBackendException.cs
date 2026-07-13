using NumericsSharp.Mkl.Native;

namespace NumericsSharp.Mkl;

public sealed class MklBackendException : Exception
{
    public MklBackendException(int statusCode, string statusName)
        : this(
            statusCode,
            statusName,
            operation: null,
            phase: null,
            matrixType: null,
            order: null,
            nonZeroCount: null,
            pardisoErrorCode: null) { }

    public MklBackendException(
        int statusCode,
        string statusName,
        string? operation,
        int? phase,
        string? matrixType,
        int? order,
        int? nonZeroCount,
        int? pardisoErrorCode)
        : base(CreateMessage(statusCode, statusName, operation, phase, matrixType, order, nonZeroCount, pardisoErrorCode))
    {
        this.StatusCode = statusCode;
        this.StatusName = statusName;
        this.Operation = operation;
        this.Phase = phase;
        this.MatrixType = matrixType;
        this.Order = order;
        this.NonZeroCount = nonZeroCount;
        this.PardisoErrorCode = pardisoErrorCode;
    }

    public int StatusCode { get; }
    public string StatusName { get; }
    public string? Operation { get; }

    public int? Phase { get; }
    public string? MatrixType { get; }

    public int? Order { get; }
    public int? NonZeroCount { get; }
    public int? PardisoErrorCode { get; }

    internal static void ThrowIfFailed(MklNativeStatus status)
        => ThrowIfFailed(
            status,
            operation: null,
            phase: null,
            matrixType: null,
            order: null,
            nonZeroCount: null,
            pardisoErrorCode: null);

    internal static void ThrowIfFailed(
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

    private static string CreateMessage(
        int statusCode,
        string statusName,
        string? operation,
        int? phase,
        string? matrixType,
        int? order,
        int? nonZeroCount,
        int? pardisoErrorCode)
    {
        var message = $"MKL backend failed with status '{statusName}' ({statusCode}).";

        if (operation is not null)
        {
            message += $" Operation: {operation}.";
        }

        if (phase is not null)
        {
            message += $" Phase: {phase}.";
        }

        if (pardisoErrorCode is not null)
        {
            message += $" PARDISO error code: {pardisoErrorCode}.";
        }

        if (matrixType is not null)
        {
            message += $" Matrix type: {matrixType}.";
        }

        if (order is not null)
        {
            message += $" Order: {order}.";
        }

        if (nonZeroCount is not null)
        {
            message += $" Nonzeros: {nonZeroCount}.";
        }

        return message;
    }
}