/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "ParallelLoop.h"
#include <ppl.h>
#include <atomic>
#include <vector>
#include <functional>
#include "Task.h"
#include <gc.h>
#include <forward_list>
#include "TaskScheduler.h"

struct iterableIterator :std::iterator<std::forward_iterator_tag, void*> {

	int64_t num;
	bool(*tryGetNext)(void*, void**);
	void(*cpIterator)(void*, void**);
	void* iterator;
	void* curr;
	void* currCpy;
	iterableIterator() {
		num = -1;
		tryGetNext = 0;
		iterator = 0;
		curr = currCpy = nullptr;
		cpIterator = nullptr;
	}
	iterableIterator(const iterableIterator&other) {
		tryGetNext = other.tryGetNext;
		if (other.cpIterator != nullptr)
			other.cpIterator(other.iterator, &iterator);
		else
			iterator = other.iterator;
		cpIterator = other.cpIterator;
		curr = other.curr;
		currCpy = curr;
		num = other.num;
	}
	iterableIterator(bool(*_tryGetNext)(void*, void**), void* it, void(*cpIterator)(void*, void**) = nullptr) {
		tryGetNext = _tryGetNext;
		iterator = it;
		num = 0;
		num = tryGetNext(it, &curr) ? 0 : -1;
		currCpy = curr;
		this->cpIterator = cpIterator;
	}
	~iterableIterator() {
		num = -1;
		tryGetNext = 0;
		iterator = 0;
		curr = 0;
		currCpy = 0;
		cpIterator = 0;
	}
	bool operator==(const iterableIterator& other) {
		if (other.num >= 0) {
			return other.num == num
				&& other.tryGetNext == tryGetNext
				&& other.iterator == iterator;
		}
		else
			return num < 0;
	}
	bool operator!=(const iterableIterator& other) {
		return !(*this == other);
	}
	void *& operator*() {
		//currCpy = curr;
		//return currCpy;
		return curr;
	}
	iterableIterator& operator++() {
		if (tryGetNext(iterator, &curr)) {
			num++;
		}
		else {
			num = -1;
		}
		return *this;
	}
	iterableIterator operator++(int) {
		auto ret = *this;
		++*this;
		return ret;
	}
	iterableIterator& operator=(const iterableIterator&other) {
		this->iterableIterator::iterableIterator(other);
		return *this;
	}
};
struct structuredTaskGroupTask {
	_Task task;
	concurrency::structured_task_group tasks;
	concurrency::task_handle<std::function<void()>> cancelTask;
};
struct structuredTaskGroupTaskT {
	_TaskT task;
	concurrency::structured_task_group tasks;
	concurrency::task_handle<std::function<void()>> cancelTask;
};
void structuredTaskGroupTaskFinalizer(void* _task, void*) {
	structuredTaskGroupTask* task = (structuredTaskGroupTask*)_task;
	task->tasks.cancel();
	task->tasks.wait();
	task->cancelTask.~task_handle();
	task->tasks.~structured_task_group();
}
void structuredTaskGroupTaskTFinalizer(void*_task, void*) {
	structuredTaskGroupTaskT* task = (structuredTaskGroupTaskT*)_task;
	task->tasks.cancel();
	task->tasks.wait();
	task->cancelTask.~task_handle();
	task->tasks.~structured_task_group();
	task->task.complete.value.store(Optional(), std::memory_order_relaxed);
}
struct taskGroupTask {
	_Task task;
	concurrency::task_group tasks;
	std::atomic_uint numComplete;
	std::atomic_uint maxNum;
	bool canFinalize;
};
struct taskGroupTaskT {
	_TaskT task;
	concurrency::task_group tasks;
};
void taskGroupTaskFinalizer(void* _task, void*) {
	taskGroupTask* task = (taskGroupTask*)_task;
	task->tasks.cancel();
	task->tasks.wait();
	task->tasks.~task_group();
}
void taskGroupTaskTFinalizer(void*_task, void*) {
	taskGroupTaskT* task = (taskGroupTaskT*)_task;
	task->tasks.cancel();
	task->tasks.wait();
	task->tasks.~task_group();
	task->task.complete.value.store(Optional(1, nullptr), std::memory_order_relaxed);
}


FB_INTERNAL(void) parallelLoop(int32_t start, int32_t count, void(*body)(uint32_t, const void*), const void* args)
{
	concurrency::parallel_for(start, start + count, 1,
		[body, args](uint32_t i) {
		body(i, args);
	});
}

FB_INTERNAL(void) parallelForLoop(bool(*tryGetNext)(void *, void **), void * iterator, void(*body)(void *, void*), void* ctx)
{

	/*void* arg;
	std::vector<concurrency::task_handle<std::function<void()>>> taskPool;

	while (tryGetNext(iterator, &arg)) {
		taskPool.emplace_back(concurrency::make_task([arg, body]() {body(arg); }));
		tasks.run(taskPool.back());
	}*/
	//concurrency::parallel_for_each(iterableIterator(tryGetNext, iterator), iterableIterator(), [body, ctx](void* arg) {body(arg, ctx); });
	concurrency::structured_task_group tasks;
	std::forward_list < concurrency::task_handle<std::function<void()>>> task_handles;
	void* curr = 0;
	while (tryGetNext(iterator, &curr)) {
		task_handles.push_front(concurrency::make_task((std::function<void()>)[curr, body, ctx]() {
			body(curr, ctx);
		}));
		tasks.run(task_handles.front());
	}
	tasks.wait();
}

FB_INTERNAL(void)* parallelLoopAnyValue(bool(*tryGetNext)(void *, void **), void * iterator, void *(*body)(void *, void*), void* ctx)
{
	// parallel-foreach with cancellation
	std::atomic<void*> ret = nullptr;
	concurrency::structured_task_group tasks;
	std::forward_list < concurrency::task_handle<std::function<void()>>> task_handles;

	void* curr = 0;
	while (tryGetNext(iterator, &curr)) {
		task_handles.push_front((std::function<void()>)[curr, body, ctx, &tasks, &ret]() {
			auto local_ret = body(curr, ctx);
			void* exp = nullptr;
			if (ret.compare_exchange_strong(exp, local_ret, std::memory_order_release, std::memory_order_relaxed))
				tasks.cancel();
		});
		tasks.run(task_handles.front());
	}
	if (tasks.wait() == concurrency::canceled)
		return ret.load(std::memory_order_acquire);
	return nullptr;
}
FB_INTERNAL(bool) parallelLoopCondAnyValue(bool(*tryGetNext)(void *, void **), void * iterator, bool(*body)(void *, void *, void **), void * ctx, void ** result)
{
	// parallel-foreach with cancellation
	std::atomic<void*> ret = nullptr;
	concurrency::structured_task_group tasks;
	std::forward_list < concurrency::task_handle<std::function<void()>>> task_handles;

	void* curr = 0;
	while (tryGetNext(iterator, &curr)) {
		task_handles.push_front((std::function<void()>)[curr, body, ctx, &tasks, &ret]() {
			void* local_res;
			if (body(curr, ctx, &local_res)) {
				void* exp = nullptr;
				if (ret.compare_exchange_strong(exp, local_res, std::memory_order_release, std::memory_order_relaxed))
					tasks.cancel();
			}
		});
		tasks.run(task_handles.front());
	}
	if (tasks.wait() == concurrency::canceled) {
		*result = ret.load(std::memory_order_acquire);
		return true;
	}
	return false;
}
FB_INTERNAL(bool) parallelLoopAny(bool(*tryGetNext)(void *, void **), void * iterator, bool(*body)(void *, void*), void* ctx)
{
	// parallel-foreach with cancellation

	concurrency::structured_task_group tasks;
	std::forward_list < concurrency::task_handle<std::function<void()>>> task_handles;

	void* curr = 0;
	while (tryGetNext(iterator, &curr)) {
		task_handles.push_front((std::function<void()>)[curr, body, ctx, &tasks]() {
			if (body(curr, ctx))
				tasks.cancel();
		});
		tasks.run(task_handles.front());
	}
	return tasks.wait() == concurrency::canceled;
}

FB_INTERNAL(void)* parallelReduce(void ** values, uint32_t valuec, void * seed, void *(*reducer)(void *, void *))
{
	return concurrency::parallel_reduce(values, values + valuec, seed, reducer);
}

FB_INTERNAL(_Task)* parallelForLoopAnyAsync(bool(*tryGetNext)(void *, void **), void * iterator, void(*body)(void *, void*), void* ctx)
{
	taskGroupTask* ret = GC_NEW(taskGroupTask);
	GC_register_finalizer(ret, taskGroupTaskFinalizer, 0, 0, 0);
	ret->task.coro_resume = 0;
	ret->task.complete = 0;
	ret->tasks.task_group::task_group();

	void* curr;
	while (tryGetNext(iterator, &curr)) {
		ret->tasks.run([body, ret, ctx, curr]() {
			body(curr, ctx);
			if (event_set(&ret->task.complete)) {
				ret->tasks.cancel();
				// continuations
				auto nod = ret->task.awaiters.load(std::memory_order_acquire);
				auto pool = getThreadPool();
				while (nod != nullptr) {
					//nod->coro_resume(nod->args);
					runAsyncTask(nod->t, pool);
					nod = nod->next;
				}
			}
		});
	}

	return &ret->task;
}

FB_INTERNAL(_TaskT)* parallelForLoopValueAsync(bool(*tryGetNext)(void *, void **), void * iterator, void *(*body)(void *, void*), void* ctx)
{
	taskGroupTaskT* ret = GC_NEW(taskGroupTaskT);
	GC_register_finalizer(ret, taskGroupTaskTFinalizer, 0, 0, 0);
	ret->task.coro_resume = 0;
	ret->task.complete = Optional();
	ret->tasks.task_group::task_group();

	void* curr;
	while (tryGetNext(iterator, &curr)) {
		ret->tasks.run([body, ret, ctx, curr]() {
			auto local_res = body(curr, ctx);
			if (value_event_set(&ret->task.complete, local_res)) {
				ret->tasks.cancel();
				// continuations
				auto nod = ret->task.awaiters.load(std::memory_order_acquire);
				auto pool = getThreadPool();
				while (nod != nullptr) {
					//nod->coro_resume(nod->args);
					runAsyncTask(nod->t, pool);
					nod = nod->next;
				}
			}
		});
	}
	return &ret->task;
}

FB_INTERNAL(_Task)* parallelForLoopAsync(bool(*tryGetNext)(void *, void **), void * iterator, void(*body)(void *, void*), void* ctx)
{

	taskGroupTask* ret = GC_NEW(taskGroupTask);
	GC_register_finalizer(ret, taskGroupTaskFinalizer, 0, 0, 0);
	ret->task.coro_resume = 0;
	ret->task.complete = 0;
	ret->numComplete.store(0, std::memory_order_relaxed);
	ret->canFinalize = false;
	ret->tasks.task_group::task_group();

	auto pool = getThreadPool();

	void* curr;
	uint32_t maxNum = 0;
	while (tryGetNext(iterator, &curr)) {
		ret->tasks.run([body, ret, ctx, curr]() {
			body(curr, ctx);
			auto num = ret->numComplete.fetch_add(1, std::memory_order_acq_rel) + 1;
			if (ret->canFinalize && num >= ret->maxNum.load(std::memory_order_acquire)) {
				auto nod = ret->task.awaiters.exchange(nullptr, std::memory_order_acq_rel);
				event_set(&ret->task.complete);
				// continuations

				auto pool = getThreadPool();
				while (nod != nullptr) {
					runAsyncTask(nod->t, pool);
					nod = nod->next;
				}
			}
		});
		maxNum++;
	}

	// continuations
	ret->maxNum.store(maxNum, std::memory_order_release);
	ret->canFinalize = true;
	if (ret->numComplete.load(std::memory_order_acquire) >= maxNum) {
		auto nod = ret->task.awaiters.exchange(nullptr, std::memory_order_acq_rel);
		event_set(&ret->task.complete);
		while (nod != nullptr) {
			runAsyncTask(nod->t, pool);
			nod = nod->next;
		}
	}

	return &ret->task;
}

