#pragma once
#include <vector>
#include <unordered_map>
#include <unordered_set>
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

typedef void**(*visitor_t)(void*, uint32_t*);
typedef void(*dtor_t)(void*);
typedef struct {
	void* obj;
	bool marked : 1;
	bool isArray : 1;
	dtor_t dtor;
	//visitor_t vis;
	uint16_t subRootCount;
	uint32_t* subRootOffsets;
}gc_obj;
typedef struct {
	void** start;
	size_t count;
} array_t;



std::vector<gc_obj*> gc_objects = std::vector<gc_obj*>();
std::vector<gc_obj*> old_gc_objects = std::vector<gc_obj*>();
std::vector< std::unordered_set<void*>> protectedObjs = std::vector<std::unordered_set<void*>>();
std::unordered_map<void*, gc_obj*> object_map = std::unordered_map<void*, gc_obj*>();
std::vector<std::vector<void**>> gc_roots = std::vector<std::vector<void**>>(1);

size_t maximum_objects = 2, maximum_old_objects = 4;

inline void removeGCRootAt(size_t index);
template<typename T>
inline void removeAt(std::vector<T> & vec, size_t index);
void finalize(gc_obj * obj);
inline void markAll();
inline void sweep(void* protect);
inline void sweep_old(void* protect);
FB_INTERNAL(void) register_gc_root(void** address);

FB_INTERNAL(void)* new_gc_obj(uint32_t numBytes, dtor_t dtor, uint32_t* vis, uint16_t subRootCount);
FB_INTERNAL(void)* new_gc_arr(uint32_t elemSize, uint16_t offset, uint16_t count);
FB_INTERNAL(void) collect_if_necessary(void* protect);
FB_INTERNAL(void) collect(void* protect);
FB_INTERNAL(void) beginFunction();
FB_INTERNAL(void) endFunction(void* protect);
FB_INTERNAL(void) gc_cleanup();
template<typename T>
inline void removeAt(std::vector<T>& vec, size_t index)
{
	vec[index] = vec.back();
	vec.pop_back();
}
