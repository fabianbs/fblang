#pragma once
#include <gc.h>
template<typename T> T* gc_newT() {
	auto ret = GC_MALLOC(sizeof(T));
	memset(ret, 0, sizeof(T));
	return (T*)ret;
}
template<typename T> T* gc_newT(uint32_t count) {
	auto ret = GC_MALLOC(sizeof(T) * count);
	memset(ret, 0, sizeof(T) * count);
	return (T*)ret;
}