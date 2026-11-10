#!/bin/sh
# Builds hangingplugin.dylib as a universal (arm64 + x86_64) binary into Assets/Hang02.
set -e

cd "$(dirname "$0")"

xcrun clang++ -std=c++17 -O2 -dynamiclib \
    -arch arm64 -arch x86_64 \
    -mmacosx-version-min=11.0 \
    -install_name @rpath/hangingplugin.dylib \
    -o ../Assets/Hang02/hangingplugin.dylib \
    hangingplugin.cpp

lipo -info ../Assets/Hang02/hangingplugin.dylib
