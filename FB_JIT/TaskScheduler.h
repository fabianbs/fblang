/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <cstdint>
#include <atomic>
#include <thread>
#include "MPSCQueue.h"
#include "Task.h"
#include "ManualResetEvent.h"
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

class PoolThread {
	uint16_t id;
	mpsc_queue<Task> tasks;
	std::thread thread;
	void run();
	volatile bool isCancelled = false;
	ManualResetEvent isWaiting;
	void executeTask(Task t);
public:
	PoolThread(uint16_t id);

	void cancel();
	void enqueue(Task t);
	void enqueue(TaskT t);
	void enqueue(LoopTask t);
	void enqueue(MutableLoopTask t);
	void enqueue(ActorTask t);
    void enqueue(ReducingLoopTask t);
};
PoolThread* getThreadPool();
void runAsyncTask(Task t, PoolThread* pool);
void finalizeTask(Task t, PoolThread* pool);
void finalizeTaskT(TaskT t, void* result, PoolThread* pool);
FB_INTERNAL(void)* runAsync(bool(*task)(void*), void* args);
FB_INTERNAL(void)* runAsyncT(bool(*task)(void*, void**), void* args);
FB_INTERNAL(void)* runImmediately(bool(*task)(void*), void* args);
FB_INTERNAL(void)* runImmediatelyT(bool(*task)(void*, void**), void* args);
FB_INTERNAL(bool) runImmediatelyTask(bool(*task)(void*), void* args, void** _ret);
FB_INTERNAL(bool) runImmediatelyTaskT(bool(*task)(void*, void**), void* args, void** _ret);

FB_INTERNAL(void) runActorTaskAsync(bool(*task)(void*), void* args, void* actorContext, void** _ret);
FB_INTERNAL(void) runActorTaskTAsync(bool(*task)(void*, void**), void* args, void* actorContext, void** _ret);

FB_INTERNAL(void)* runLoopAsync(bool(*body)(void*, uint64_t, uint64_t), void* args, uint64_t start, uint64_t count, uint64_t blockSize);
FB_INTERNAL(void)* runLoopCoroutineAsync(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize);
FB_INTERNAL(void) runLoopCoroutineAsyncTask(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize, void** ret);
FB_INTERNAL(void)* runLoopCoroutineAsyncOffs(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize, uint32_t mutThisTaskOffset);
FB_INTERNAL(void)* reduceAsync(void* (*body)(char*, uint64_t, uint64_t), void*(*reducer)(void*,void*), char* values, uint32_t valuec, uint64_t blockSize, void* seed);

FB_INTERNAL(void)* getCompletedTask();
FB_INTERNAL(void)* getCompletedTaskT(void* result);
FB_INTERNAL(void)* waitTaskValue(void* taskt);
FB_INTERNAL(void)* getTaskValue(void* taskt);
FB_INTERNAL(void) waitTask(void* task);
FB_INTERNAL(bool) isTaskCompleted(void* task);
FB_INTERNAL(bool) isTask(void* task);
uint16_t getCurrentThreadID();

FB_INTERNAL(bool) taskAwaiterEnqueue(Task t, Task awaiter);