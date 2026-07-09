namespace NumericsSharp.Mkl.Native;

internal sealed class MklNativeException : Exception
{
    public MklNativeException(MklNativeStatus status)
        : base($"MKL native backend failed with status '{status}'.")
    {
        Status = status;
    }

    public MklNativeStatus Status { get; }

    public static void ThrowIfFailed(MklNativeStatus status)
    {
        if (status != MklNativeStatus.Success)
        {
            throw new MklNativeException(status);
        }
    }
}
