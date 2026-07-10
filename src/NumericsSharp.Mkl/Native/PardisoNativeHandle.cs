using System.Runtime.InteropServices;

namespace NumericsSharp.Mkl.Native;

internal sealed class PardisoNativeHandle : SafeHandle
{
    private PardisoNativeHandle() : base(nint.Zero, ownsHandle: true) { }

    public PardisoNativeHandle(nint handle, bool ownsHandle) : base(nint.Zero, ownsHandle)
    {
        this.SetHandle(handle);
    }

    public override bool IsInvalid => this.handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        return PardisoNativeMethods.Destroy(this.handle) == MklNativeStatus.Success;
    }
}