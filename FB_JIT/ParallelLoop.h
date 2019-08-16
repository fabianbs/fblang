#pragma once
#include <cstdint>
#ifndef FB_INTERNALS
#define FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

struct _Task;
struct _TaskT;

FB_INTERNAL(void) parallelLoop(int32_t start, int32_t count, void(*body)(uint32_t,const void*),const void* args);
FB_INTERNAL(void) parallelForLoop(bool(*tryGetNext)(void*, void**), void* iterator, void(*body)(void*,void*), void* ctx);

FB_INTERNAL(void) * parallelLoopAnyValue(bool(*tryGetNext)(void*, void**), void* iterator, void*(*body)(void*,void*),void* ctx);
FB_INTERNAL(bool) parallelLoopCondAnyValue(bool(*tryGetNext)(void*, void**), void* iterator, bool(*body)(void*, void*, void**), void* ctx, void** result);
FB_INTERNAL(bool)  parallelLoopAny(bool(*tryGetNext)(void*, void**), void* iterator, bool(*body)(void*,void*), void* ctx);
FB_INTERNAL(void)* parallelReduce(void** values, uint32_t valuec, void* seed, void*(*reducer)(void*,void*));
FB_INTERNAL(_Task)* parallelForLoopAnyAsync(bool(*tryGetNext)(void*, void**),void* iterator, void(*body)(void*,void*), void* ctx);
FB_INTERNAL(_TaskT)* parallelForLoopValueAsync(bool(*tryGetNext)(void*, void**), void* iterator, void*(*body)(void*,void*),void* ctx);

FB_INTERNAL(_Task)* parallelForLoopAsync(bool(*tryGetNext)(void*, void**), void* iterator, void(*body)(void*,void*),void* ctx);