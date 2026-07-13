namespace NumericsSharp.Mkl.Interop;

internal enum MklNativeStatus
{
    Success = 0,
    InvalidArgument = 1,
    MklError = 2,
    OutOfMemory = 3,
    UnknownError = 255
}
