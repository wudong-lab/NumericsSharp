cmake_minimum_required(VERSION 3.20)

project(NumericsSharp.MklNative LANGUAGES CXX)

add_library(NumericsSharp.MklNative SHARED
    src/PardisoNative.cpp
)

target_compile_features(NumericsSharp.MklNative PRIVATE cxx_std_17)

target_include_directories(NumericsSharp.MklNative
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

if (MSVC)
    target_compile_options(NumericsSharp.MklNative PRIVATE /W4 /permissive-)
endif()
