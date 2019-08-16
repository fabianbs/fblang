#pragma once
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
#include <cstdint>
struct _Task;
FB_INTERNAL(void) thread_sleep(uint32_t millis);
FB_INTERNAL(_Task)* delay(uint32_t millis);