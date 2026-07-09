#pragma once

#ifdef _WIN32
#define NS_MKL_NATIVE_API extern "C" __declspec(dllexport)
#else
#define NS_MKL_NATIVE_API extern "C"
#endif

enum class NsMklNativeStatus : int
{
    Success = 0,
    InvalidArgument = 1,
    MklError = 2,
    OutOfMemory = 3,
    UnknownError = 255
};

enum class NsPardisoMatrixType : int
{
    RealStructurallySymmetric = 1,
    RealSymmetricPositiveDefinite = 2,
    RealSymmetricIndefinite = -2,
    RealUnsymmetric = 11
};

struct NsPardisoHandle;

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_create(NsPardisoHandle** handle);

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_destroy(NsPardisoHandle* handle);

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_analyze(
    NsPardisoHandle* handle,
    int order,
    int nonZeroCount,
    const int* rowPointers,
    const int* columns,
    NsPardisoMatrixType matrixType);

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_factorize(
    NsPardisoHandle* handle,
    const double* values);

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_solve(
    NsPardisoHandle* handle,
    const double* rightHandSide,
    double* solution,
    int rightHandSideCount);
