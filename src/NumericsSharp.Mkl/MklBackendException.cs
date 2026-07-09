namespace NumericsSharp.Mkl;

public sealed class MklBackendException : Exception
{
    public MklBackendException(int statusCode, string statusName)
        : base($"MKL backend failed with status '{statusName}' ({statusCode}).")
    {
        StatusCode = statusCode;
        StatusName = statusName;
    }

    public int StatusCode { get; }

    public string StatusName { get; }
}
