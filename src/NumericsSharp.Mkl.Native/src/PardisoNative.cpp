#include "NumericsSharpMklNative.h"

#include <new>
#include <vector>

#ifdef NUMERICS_SHARP_USE_MKL
#pragma warning(push)
#pragma warning(disable : 4819)
#include <mkl.h>
#pragma warning(pop)

static_assert(sizeof(MKL_INT) == sizeof(int), "NumericsSharp native backend currently requires MKL LP64 integer size.");
#endif

struct NsPardisoHandle
{
    int order = 0;
    int nonZeroCount = 0;
    NsPardisoMatrixType matrixType = NsPardisoMatrixType::RealSymmetricPositiveDefinite;
    bool analyzed = false;
    bool factorized = false;

#ifdef NUMERICS_SHARP_USE_MKL
    void* internalSolverMemory[64] = {};
    MKL_INT iparm[64] = {};
    std::vector<MKL_INT> rowPointers;
    std::vector<MKL_INT> columns;
    std::vector<double> values;
#endif
};

#ifdef NUMERICS_SHARP_USE_MKL
namespace
{
    void initialize_iparm(NsPardisoHandle* handle)
    {
        for (auto& value : handle->iparm)
        {
            value = 0;
        }

        handle->iparm[0] = 1;  // Use custom iparm values.
        handle->iparm[1] = 2;  // Fill-in reordering from METIS.
        handle->iparm[7] = 2;  // Iterative refinement steps.
        handle->iparm[9] = 13; // Pivot perturbation.
        handle->iparm[17] = -1;
        handle->iparm[18] = -1;
        handle->iparm[34] = 0; // One-based CSR indexing.
    }

    NsMklNativeStatus call_pardiso(
        NsPardisoHandle* handle,
        MKL_INT phase,
        const int* rowPointers,
        const int* columns,
        const double* values,
        const double* rightHandSide,
        double* solution,
        int rightHandSideCount)
    {
        MKL_INT maxfct = 1;
        MKL_INT mnum = 1;
        MKL_INT matrixType = static_cast<MKL_INT>(handle->matrixType);
        MKL_INT order = static_cast<MKL_INT>(handle->order);
        MKL_INT nrhs = static_cast<MKL_INT>(rightHandSideCount);
        MKL_INT msglvl = 0;
        MKL_INT error = 0;
        double dummyValue = 0.0;

        auto* mutableValues = const_cast<double*>(
            values != nullptr
                ? values
                : (!handle->values.empty() ? handle->values.data() : &dummyValue));
        auto* mutableRowPointers = const_cast<MKL_INT*>(
            rowPointers != nullptr
                ? reinterpret_cast<const MKL_INT*>(rowPointers)
                : handle->rowPointers.data());
        auto* mutableColumns = const_cast<MKL_INT*>(
            columns != nullptr
                ? reinterpret_cast<const MKL_INT*>(columns)
                : handle->columns.data());
        auto* mutableRightHandSide = const_cast<double*>(rightHandSide != nullptr ? rightHandSide : &dummyValue);
        auto* mutableSolution = solution != nullptr ? solution : &dummyValue;

        pardiso(
            handle->internalSolverMemory,
            &maxfct,
            &mnum,
            &matrixType,
            &phase,
            &order,
            mutableValues,
            mutableRowPointers,
            mutableColumns,
            nullptr,
            &nrhs,
            handle->iparm,
            &msglvl,
            mutableRightHandSide,
            mutableSolution,
            &error);

        return error == 0 ? NsMklNativeStatus::Success : NsMklNativeStatus::MklError;
    }

    void release_pardiso(NsPardisoHandle* handle)
    {
        if (handle == nullptr)
        {
            return;
        }

        MKL_INT phase = -1;
        MKL_INT maxfct = 1;
        MKL_INT mnum = 1;
        MKL_INT matrixType = static_cast<MKL_INT>(handle->matrixType);
        MKL_INT order = handle->order > 0 ? static_cast<MKL_INT>(handle->order) : 1;
        MKL_INT nrhs = 1;
        MKL_INT msglvl = 0;
        MKL_INT error = 0;
        double dummyValue = 0.0;
        MKL_INT dummyPointer[] = { 1, 1 };
        MKL_INT dummyColumn[] = { 1 };

        pardiso(
            handle->internalSolverMemory,
            &maxfct,
            &mnum,
            &matrixType,
            &phase,
            &order,
            &dummyValue,
            dummyPointer,
            dummyColumn,
            nullptr,
            &nrhs,
            handle->iparm,
            &msglvl,
            &dummyValue,
            &dummyValue,
            &error);
    }
}
#endif

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
#ifdef NUMERICS_SHARP_USE_MKL
        initialize_iparm(*handle);
#endif
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
#ifdef NUMERICS_SHARP_USE_MKL
    release_pardiso(handle);
#endif
    delete handle;
    return NsMklNativeStatus::Success;
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_mkl_set_thread_count(int threadCount)
{
    if (threadCount <= 0)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

#ifdef NUMERICS_SHARP_USE_MKL
    mkl_set_num_threads_local(threadCount);
#endif
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

#ifdef NUMERICS_SHARP_USE_MKL
    try
    {
        handle->rowPointers.assign(rowPointers, rowPointers + order + 1);
        handle->columns.assign(columns, columns + nonZeroCount);
    }
    catch (const std::bad_alloc&)
    {
        return NsMklNativeStatus::OutOfMemory;
    }

#endif

    handle->analyzed = true;
    handle->factorized = false;

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

#ifdef NUMERICS_SHARP_USE_MKL
    try
    {
        handle->values.assign(values, values + handle->nonZeroCount);
    }
    catch (const std::bad_alloc&)
    {
        return NsMklNativeStatus::OutOfMemory;
    }

    auto status = call_pardiso(
        handle,
        12,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        1);
    if (status != NsMklNativeStatus::Success)
    {
        return status;
    }

    handle->factorized = true;
    return NsMklNativeStatus::Success;
#else
    return NsMklNativeStatus::MklError;
#endif
}

NS_MKL_NATIVE_API NsMklNativeStatus ns_pardiso_solve(
    NsPardisoHandle* handle,
    const double* rightHandSide,
    double* solution,
    int rightHandSideCount)
{
    if (handle == nullptr || !handle->factorized || rightHandSide == nullptr || solution == nullptr || rightHandSideCount <= 0)
    {
        return NsMklNativeStatus::InvalidArgument;
    }

#ifdef NUMERICS_SHARP_USE_MKL
    return call_pardiso(
        handle,
        33,
        nullptr,
        nullptr,
        nullptr,
        rightHandSide,
        solution,
        rightHandSideCount);
#else
    return NsMklNativeStatus::MklError;
#endif
}
