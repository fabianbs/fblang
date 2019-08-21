/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include "Task.h"
#include "ManualResetEvent.h"
#include <atomic>
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
#include "AlignedStorage.h"
//TODO:  dynamic way of determining cache-line-size
const int CACHE_LINE_SIZE = 64;

typedef typename AlignedStorage<uint32_t, CACHE_LINE_SIZE> aligned_uint32_t;
typedef typename AlignedStorage<void*, CACHE_LINE_SIZE> aligned_pvoid_t;
class TaskBarrier {
    uint32_t* reached;
    uint32_t* maxReachable;
    std::atomic_uint32_t counter;
    uint16_t numThreads, maxCounter;
    Task  task;
public:
    TaskBarrier(uint16_t _numThreads, uint32_t* _maxReachable, Task  _task);
    void reach(uint16_t id);
    void reach(uint16_t id)volatile;
    friend std::ostream& operator<<(std::ostream& os, const TaskBarrier& barrier);
};
class ReduceBarrier {
    aligned_uint32_t* reached;
    uint32_t* maxReachable;
    aligned_pvoid_t* values;
    void* (*reducer)(void*, void*);
    uint16_t numThreads, maxCounter;
    std::atomic_uint32_t counter;
    TaskT task;
public:
    ReduceBarrier(uint16_t _numThreads, uint32_t* _maxReachable, TaskT _task, void* (*_reducer)(void*, void*), void* seed);
    void reach(uint16_t id, void* value);
};