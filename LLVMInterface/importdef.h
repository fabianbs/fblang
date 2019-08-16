#pragma once
#ifdef LLVM_Interface_Export_Symbols
#define EXTERN_API(x) extern "C" x __declspec(dllexport)
#else
#define EXTERN_API(x) extern "C" x __declspec(dllimport)
#endif
