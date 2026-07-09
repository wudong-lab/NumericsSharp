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
