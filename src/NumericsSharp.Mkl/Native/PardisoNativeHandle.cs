using System.Runtime.InteropServices;

namespace NumericsSharp.Mkl.Native;

internal sealed class PardisoNativeHandle : SafeHandle
{
    private PardisoNativeHandle()
        : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public PardisoNativeHandle(IntPtr handle, bool ownsHandle)
        : base(IntPtr.Zero, ownsHandle)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return PardisoNativeMethods.Destroy(handle) == MklNativeStatus.Success;
    }
}
