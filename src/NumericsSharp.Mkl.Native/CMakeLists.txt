cmake_minimum_required(VERSION 3.20)

project(NumericsSharp.Mkl.Native LANGUAGES CXX)

add_library(NumericsSharp.Mkl.Native SHARED
    src/PardisoNative.cpp
)

target_compile_features(NumericsSharp.Mkl.Native PRIVATE cxx_std_17)

target_include_directories(NumericsSharp.Mkl.Native
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

if (MSVC)
    target_compile_options(NumericsSharp.Mkl.Native PRIVATE /W4 /permissive-)
endif()

set(MKL_ROOT "$ENV{MKLROOT}")
set(MKL_CANDIDATE_ROOTS
    "${MKL_ROOT}/latest"
    "${MKL_ROOT}"
)

foreach(candidate_root IN LISTS MKL_CANDIDATE_ROOTS)
    if (EXISTS "${candidate_root}/include/mkl.h" AND EXISTS "${candidate_root}/lib/mkl_core.lib")
        set(NUMERICS_SHARP_MKL_ROOT "${candidate_root}")
        break()
    endif()
endforeach()

if (NUMERICS_SHARP_MKL_ROOT)
    message(STATUS "Building with Intel MKL: ${NUMERICS_SHARP_MKL_ROOT}")
    target_compile_definitions(NumericsSharp.Mkl.Native PRIVATE NUMERICS_SHARP_USE_MKL)
    target_include_directories(NumericsSharp.Mkl.Native PRIVATE "${NUMERICS_SHARP_MKL_ROOT}/include")
    target_link_directories(NumericsSharp.Mkl.Native PRIVATE "${NUMERICS_SHARP_MKL_ROOT}/lib")
    target_link_libraries(NumericsSharp.Mkl.Native PRIVATE
        mkl_intel_lp64
        mkl_sequential
        mkl_core
    )
else()
    message(STATUS "Intel MKL was not found. Building native PARDISO stub.")
endif()
