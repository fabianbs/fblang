/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "SimpleGC.h"
#include <queue>

inline void removeGCRootAt(size_t index)
{
	finalize(gc_objects[index]);
	removeAt(gc_objects, index);
}

void finalize(gc_obj* obj)
{
	printf("free %p\n", obj->obj);
	if (obj->dtor)
		obj->dtor(obj->obj);
	free(obj->obj);
	free(obj);
}

inline void markAll()
{
	std::queue<void*> queue;
	for (auto aktFnRoots : gc_roots) {

		for (auto mem : aktFnRoots) {
			queue.push(*mem);
			do {
				auto elem = queue.front();

				queue.pop();

				//register auto elem = akt.start[k];
				auto it = object_map.find(elem);
				if (it != object_map.end()) {
					if (!it->second->marked) {
						it->second->marked = true;
						//uint32_t anz = 0;
						//queue.push({ it->second->vis(elem,&anz),anz });
						auto subRoots = it->second->subRootOffsets;
						auto src = it->second->subRootCount;
						if (!it->second->isArray) {
							for (unsigned short i = 0; i < src; i++, subRoots++) {
								queue.push(*subRoots + ((char*) elem));
							}
						}
						else {
							auto sr = (void**) ((char*) elem + (uint32_t) subRoots);
							for (unsigned short i = 0; i < src; i++, subRoots++) {
								queue.push(*sr);
							}
						}
					}
				}

			} while (!queue.empty());

		}
	}
}

inline void sweep(void* protect)
{
	while (!gc_objects.empty()) {
		auto akt = gc_objects.back();
		bool collected = false;
		if (!akt->marked && akt->obj != protect)
		{
			bool isProtected = false;
			for (auto& set : protectedObjs)
			{
				if (set.find(akt->obj) != set.end())
				{
					isProtected = true;
					break;
				}
			}
			if (!isProtected) {
				finalize(akt);
				collected = true;
			}
		}

		if (!collected) {
			old_gc_objects.push_back(akt);
		}
		gc_objects.pop_back();
	}

}

inline void sweep_old(void * protect)
{
	for (size_t i = 0; i < old_gc_objects.size();) {
		auto akt = old_gc_objects[i];
		if (!akt->marked && akt->obj != protect)
		{
			bool isProtected = false;
			for (auto& set : protectedObjs)
			{
				if (set.find(akt->obj) != set.end())
				{
					isProtected = true;
					break;
				}
			}
			if (!isProtected) {
				finalize(akt);
				removeAt(old_gc_objects, i);
			}
			else
				i++;
		}
		else
			i++;
	}
	maximum_objects = (1 + old_gc_objects.size());
	maximum_old_objects = maximum_objects << 1;

}

FB_INTERNAL(void) register_gc_root(void ** address)
{
	gc_roots.back().push_back(address);
}


FB_INTERNAL(void)* new_gc_obj(uint32_t numBytes, dtor_t dtor, uint32_t * vis, uint16_t subRootCount)
{
	auto obj = malloc(numBytes);
	printf("register: %p\n", obj);
	if (obj != nullptr) {
		register gc_obj* nwObj = (gc_obj*) malloc(sizeof(gc_obj));
		nwObj->dtor = dtor;
		nwObj->subRootOffsets = vis;
		nwObj->obj = obj;
		nwObj->isArray = false;
		nwObj->marked = false;
		nwObj->subRootCount = subRootCount;
		gc_objects.push_back(nwObj);
		object_map[obj] = nwObj;
	}
	return obj;
}

FB_INTERNAL(void)* new_gc_arr(uint32_t elemSize, uint16_t offset, uint16_t count)
{
	auto arr = malloc(elemSize * count);
	if (arr != nullptr) {
		register gc_obj* nwObj = (gc_obj*) malloc(sizeof(gc_obj));
		nwObj->dtor = nullptr;
		nwObj->subRootOffsets = (uint32_t*) offset;
		nwObj->obj = arr;
		nwObj->isArray = true;
		nwObj->marked = false;
		nwObj->subRootCount = count;
		gc_objects.push_back(nwObj);
		object_map[arr] = nwObj;
	}
	return arr;
}

FB_INTERNAL(void) collect_if_necessary(void* protect)
{
	if (gc_objects.size() >= maximum_objects) {
		collect(protect);
	}
}

FB_INTERNAL(void) collect(void* protect)
{
	markAll();
	sweep(protect);
	if (old_gc_objects.size() >= maximum_old_objects)
		sweep_old(protect);
}

FB_INTERNAL(void) beginFunction()
{
	protectedObjs.push_back(std::unordered_set<void*>());
	gc_roots.push_back(std::vector<void**>());
}

FB_INTERNAL(void) endFunction(void* protect)
{
	protectedObjs.pop_back();
	gc_roots.pop_back();
	collect_if_necessary(protect);

}

FB_INTERNAL(void) gc_cleanup()
{
	//TODO Beachte Abh�ngigkeiten!!

	for (auto akt : gc_objects) {

		finalize(akt);
	}
	gc_objects.clear();
	for (auto akt : old_gc_objects) {

		finalize(akt);
	}
	old_gc_objects.clear();
}

