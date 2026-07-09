namespace NumericsSharp.Mkl.Native;

internal static class PardisoNativeApi
{
    public static PardisoNativeHandle Create()
    {
        MklNativeException.ThrowIfFailed(PardisoNativeMethods.Create(out var handle));
        return new PardisoNativeHandle(handle, ownsHandle: true);
    }
}
