namespace NumericsSharp.Mkl.Native;

internal static class MklNativeException
{
    public static void ThrowIfFailed(MklNativeStatus status)
    {
        if (status != MklNativeStatus.Success)
        {
            throw new MklBackendException((int)status, status.ToString());
        }
    }
}
