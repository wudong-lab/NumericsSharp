#include "NumericsSharpMklNative.h"

#include <new>

struct NsPardisoHandle
{
    int order = 0;
    int nonZeroCount = 0;
    NsPardisoMatrixType matrixType = NsPardisoMatrixType::RealSymmetricPositiveDefinite;
    bool analyzed = false;
};

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_create(NsPardisoHandle** handle)
{
    if (handle == nullptr)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

    *handle = nullptr;

    try
    {
        *handle = new NsPardisoHandle();
        return NsMklNativeStatus::Success;
    }
    catch (const std::bad_alloc&)
    {
        return NsMklNativeStatus::OutOfMemory;
    }
    catch (...)
    {
        return NsMklNativeStatus::UnknownError;
    }
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_destroy(NsPardisoHandle* handle)
{
    delete handle;
    return NsMklNativeStatus::Success;
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_analyze(
    NsPardisoHandle* handle,
    int order,
    int nonZeroCount,
    const int* rowPointers,
    const int* columns,
    NsPardisoMatrixType matrixType)
{
    if (handle == nullptr || order <= 0 || nonZeroCount < 0 || rowPointers == nullptr)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

    if (nonZeroCount > 0 && columns == nullptr)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

    handle->order = order;
    handle->nonZeroCount = nonZeroCount;
    handle->matrixType = matrixType;
    handle->analyzed = true;

    return NsMklNativeStatus::Success;
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_factorize(
    NsPardisoHandle* handle,
    const double* values)
{
    if (handle == nullptr || !handle->analyzed || values == nullptr)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

    return NsMklNativeStatus::MklError;
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_solve(
    NsPardisoHandle* handle,
    const double* rightHandSide,
    double* solution,
    int rightHandSideCount)
{
    if (handle == nullptr || rightHandSide == nullptr || solution == nullptr || rightHandSideCount <= 0)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

    return NsMklNativeStatus::MklError;
}
