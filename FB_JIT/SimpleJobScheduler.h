#pragma once
#include <cstdint>
#include "Actor.h"
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS



FB_INTERNAL(void) schedInit();
FB_INTERNAL(void) schedInitThreads(uint32_t threadc);
FB_INTERNAL(void) schedActor(Actor ac);