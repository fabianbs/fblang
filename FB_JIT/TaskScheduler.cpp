/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "TaskScheduler.h"
#include <cassert>
#include "TaskBarrier.h"
#include "Task.h"
#include <vector>

std::atomic< PoolThread*> threadPool = 0;
uint16_t threadCount = 0;
thread_local uint16_t insertIndex = 0;
thread_local uint16_t threadID = 0;


PoolThread::PoolThread(uint16_t id)
    :id(id), tasks(), thread([this]() {this->run(); })
{
    isWaiting.value.store(false, std::memory_order_relaxed);
}

void PoolThread::cancel() {
    if (!isCancelled) {
        isCancelled = true;
        thread.join();
    }
}

void PoolThread::enqueue(Task t)
{
    mpsc_enqueue(&tasks, t);
    event_set(&isWaiting);
    /*if ((char*)t >= 0 && (char*)t < (char*)32) {
        printf("");
    }*/
}

void PoolThread::enqueue(TaskT t)
{
    enqueue(reinterpret_cast<Task>(t));
}

void PoolThread::enqueue(LoopTask t)
{
    enqueue(reinterpret_cast<Task>(t));
}

void PoolThread::enqueue(MutableLoopTask t)
{
    enqueue(reinterpret_cast<Task>(t));
}

void PoolThread::enqueue(ActorTask t)
{
    enqueue(reinterpret_cast<Task>(t));
}

void PoolThread::enqueue(ReducingLoopTask t)
{
    enqueue(reinterpret_cast<Task>(t));
}

void PoolThread::executeTask(Task t) {
    if (t) {

        switch (t->ty) {
            case VoidTask:
                if (!t->cancelled && !t->complete.value.load(std::memory_order_relaxed)) {
                    if (t->coro_resume(t->args)) {
                        finalizeTask(t, &this[-id]);
                    }
                }
                break;
            case ValueTask: {
                //static int counter = 0;
                //printf("#%u\n", ++counter);
                auto _t = reinterpret_cast<TaskT>(t);
                if (!_t->cancelled && !_t->complete.value.load(std::memory_order::memory_order_relaxed).hasValue) {
                    void* result;
                    if (_t->coro_resume(_t->args, &result)) {
                        finalizeTaskT(_t, result, &this[-id]);
                    }
                }
                break;
            }
            case LoopTaskTy: {
                auto lt = reinterpret_cast<LoopTask>(t);
                if (!lt->cancelled) {
                    //printf("Start Iterations: from %lu, to %lu\n", lt->start, lt->start + lt->count);
                    if (lt->coro_resume(lt->args, lt->start, lt->count) && lt->barrier) {
                        lt->barrier->reach(id);
                    }
                }
                break;
            }
            case ReducingLoopTaskTy: {
                auto rt = reinterpret_cast<ReducingLoopTask>(t);
                if (!rt->cancelled) {
                    auto res = rt->body(rt->array, rt->start, rt->count);
                    if (rt->barrier)
                        rt->barrier->reach(id, res);
                }
                break;
            }
            case MutableLoopTaskTy: {
                //printf("Execute Mutable Loop Task\n");
                auto mlt = reinterpret_cast<MutableLoopTask>(t);
                int isInvalidTask = !isTask(mlt);


                if (!mlt->cancelled) {
                    //auto start = mlt->start;

                    if (mlt->coro_resume(mlt->immutableArgs, mlt->mutableArgs, &mlt->start, mlt->count)) {

                        if (mlt->barrier == nullptr) {
                            printf("ERROR: Invalid MutableLoopTask{immu: %p, mut: %p, start: %llu, count: %llu, barrier: %p}, was previously invalid: %d\n", mlt->immutableArgs, mlt->mutableArgs, mlt->start, mlt->count, mlt->barrier, isInvalidTask);
                        }
                        else {
                            //auto str = taskToString((Task)mlt);
                            //printf("Task %s finished\n", str.c_str());
                            mlt->barrier->reach(id);

                        }
                    }
                    //mlt->start = start;
                }
                break;
            }
            case ActorTaskTy: {
                auto at = reinterpret_cast<ActorTask>(t);
                auto hasTask = mpsc_dequeue(&at->ctx->queue, &t);
                if (hasTask) {
                    executeTask(t);
                    enqueue(at);
                }
                else {
                    //printf("Actor Task %p is no longer needed\n", at);
                    at->ctx->isScheduled.store(false, std::memory_order_release);
                }
                /*if (hasTask)
                    executeTask(t);
                enqueue(at);*/
                break;
            }
            case ActorVoidTaskTy: {
                executeTask(&((ActorVoidTask)t)->task);
                break;
            }
            case ActorValueTaskTy: {
                executeTask((Task) & ((ActorValueTask)t)->task);
                break;
            }

            case ErrorTask: {
                printf("Error-Task\n");
                break;
            }
        }

    }
}

void PoolThread::run()
{
    GC_stack_base bas;
    GC_get_stack_base(&bas);
    GC_register_my_thread(&bas);
    threadID = id;
    insertIndex += id;
    while (!isCancelled) {
        Task t = 0;
        while (!mpsc_dequeue(&tasks, &t)) {
            event_wait(&isWaiting);
            event_reset(&isWaiting);
        }
        executeTask(t);
    }
    GC_unregister_my_thread();
}


PoolThread* getThreadPool() {
    PoolThread* pool;
    if (!(pool = threadPool.load(std::memory_order_acquire))) {
        uint16_t n = std::thread::hardware_concurrency();
        //32;
        threadCount = n;
        pool = (PoolThread*)GC_MALLOC(n * sizeof(PoolThread));
        for (uint16_t i = 0; i < n; ++i) {
            pool[i].PoolThread::PoolThread(i);
        }
        threadPool.store(pool, std::memory_order_release);
    }
    return pool;
}
void runAsyncTask(Task t, PoolThread * pool) {
    auto index = insertIndex++ % threadCount;
    pool[index].enqueue(t);
}
void runActorTaskAsync(Task t, ActorContext ctx, PoolThread * pool) {

    if (mpsc_enqueue(&ctx->queue, t)) {
        if (!ctx->isScheduled.exchange(true, std::memory_order_acq_rel)) {
            auto at = reinterpret_cast<Task>(gcnewActorTask(ctx));
            auto str = taskToString(at);

            //printf("(Re)schedule actor task %s on thread %d\n", str.c_str(), insertIndex % threadCount);
            //std::atomic_thread_fence(std::memory_order_seq_cst);
            ctx->thread = insertIndex % threadCount;
            ctx->actorTask = at;
            runAsyncTask(at, pool);
        }
        else {
            // executeTask sees empty queue, but was preempted before it could unset isScheduled
            pool[ctx->thread].enqueue(ctx->actorTask);
        }
    }
    else {
        // make sure, that isScheduled is true, when the queue is not empty
        ctx->isScheduled.store(true, std::memory_order_release);
    }
}
void scheduleAwaiters(std::atomic<awaiter_t*> * awaiters, PoolThread * pool) {
    if (awaiters) {
        auto finishedAwaiter = getFinishedAwaiter();
        auto aw = awaiters->exchange(finishedAwaiter, std::memory_order_acq_rel);
        if (aw != finishedAwaiter) {
            if (!pool)
                pool = getThreadPool();
            while (aw && aw != finishedAwaiter) {
                auto nxt = aw->next;
                if (aw->t) {
                    switch (aw->t->ty) {
                        case LoopTaskTy:
                            pool[((LoopTask)aw->t)->thread].enqueue(aw->t);
                            break;
                        case MutableLoopTaskTy:
                            pool[((MutableLoopTask)aw->t)->thread].enqueue(aw->t);
                            break;
                        case ActorVoidTaskTy: {
                            auto avt = (ActorVoidTask)aw->t;
                            runActorTaskAsync(aw->t, avt->ctx, pool);
                            break;
                        }
                        case ActorValueTaskTy: {
                            auto avt = (ActorValueTask)aw->t;
                            runActorTaskAsync(aw->t, avt->ctx, pool);
                            break;
                        }
                        default:
                            runAsyncTask(aw->t, pool);
                            break;
                    }
                }
                aw = nxt;
            }
        }
    }
}
void finalizeTask(Task t, PoolThread * pool)
{
    if (t && t->ty == VoidTask) {
        std::atomic<awaiter_t*>* awaiters = &t->awaiters;

        event_set(&t->complete);

        scheduleAwaiters(awaiters, pool);
    }
}
void finalizeTaskT(TaskT t, void* result, PoolThread * pool)
{
    if (t && t->ty == ValueTask) {
        // printf("ValueTask finished with %llu\n", (uint64_t)result);

        value_event_set(&t->complete, result);
        std::atomic<awaiter_t*>* awaiters = &t->awaiters;

        scheduleAwaiters(awaiters, pool);
    }
}
FB_INTERNAL(void)* runAsync(bool(*task)(void*), void* args)
{
    auto ret = gcnewTask(task, args, false);
    auto pool = getThreadPool();
    runAsyncTask(ret, pool);
    return ret;
}

FB_INTERNAL(void)* runAsyncT(bool(*task)(void*, void**), void* args)
{
    auto ret = gcnewTaskT(task, args, false, nullptr);
    auto pool = getThreadPool();
    runAsyncTask(reinterpret_cast<Task>(ret), pool);
    return ret;
}

FB_INTERNAL(void)* runImmediately(bool(*task)(void*), void* args)
{
    Task ret;

    if (task(args)) {
        // when task finishes synchronously, there will be no awaiters


        ret = (Task)getCompletedTask();
    }
    else {
        ret = gcnewTask(task, args, false);
    }
    return ret;
}

FB_INTERNAL(void)* runImmediatelyT(bool(*task)(void*, void**), void* args)
{
    auto ret = gcnewTaskT(task, args, false, nullptr);
    void* result = 0;
    if (task(args, &result)) {
        // when task finishes synchronously, there will be no awaiters
        value_event_set(&ret->complete, result);

    }
    return ret;
}

FB_INTERNAL(bool) runImmediatelyTask(bool(*task)(void*), void* args, void** _ret)
{
    auto ret = gcnewTask(task, args, false);
    *_ret = ret;
    //TODO don't allocate new task when it completes synchronously
    if (task(args)) {

        event_set(&ret->complete);
        return true;
    }
    else {

        return false;
    }
}

FB_INTERNAL(bool) runImmediatelyTaskT(bool(*task)(void*, void**), void* args, void** _ret)
{
    auto ret = gcnewTaskT(task, args, false, nullptr);
    *_ret = ret;
    void* result = 0;
    if (task(args, &result)) {
        value_event_set(&ret->complete, result);
        return true;
    }
    return false;
}

FB_INTERNAL(void) runActorTaskAsync(bool(*task)(void*), void* args, void* actorContext, void** _ret)
{
    auto ret = reinterpret_cast<Task>(gcnewActorVoidTask(task, args, false, actorContext));
    *_ret = ret;
    auto pool = getThreadPool();
    auto ctx = reinterpret_cast<ActorContext>(actorContext);
    /*mpsc_enqueue(queue, ret);
    if (!isScheduled->exchange(true, std::memory_order_acq_rel)) {
        runAsyncTask(reinterpret_cast<Task>(gcnewActorTask(behavior_queue, isScheduled)), pool);
    }*/
    runActorTaskAsync(ret, ctx, pool);
}

FB_INTERNAL(void) runActorTaskTAsync(bool(*task)(void*, void**), void* args, void* actorContext, void** _ret)
{
    auto ret = reinterpret_cast<Task>(gcnewActorValueTask(task, args, false, nullptr, actorContext));
    *_ret = ret;
    auto pool = getThreadPool();
    auto ctx = reinterpret_cast<ActorContext>(actorContext);
    /*mpsc_enqueue(queue, ret);
    if (!isScheduled->exchange(true, std::memory_order_acq_rel)) {
        //printf("(Re)schedule actor task\n");
        runAsyncTask(reinterpret_cast<Task>(gcnewActorTask(behavior_queue, isScheduled)), pool);
    }*/
    runActorTaskAsync(ret, ctx, pool);
}

FB_INTERNAL(void)* runLoopAsync(bool(*body)(void*, uint64_t, uint64_t), void* args, uint64_t start, uint64_t count, uint64_t blockSize)
{
    auto ret = gcnewTask(nullptr, nullptr, false);
    auto totalWorkLoad = count / blockSize;
    auto pool = getThreadPool(); // threadCount is available only after the first getThreadPool() call
    auto workLoad = totalWorkLoad / threadCount;
    auto tail = count % blockSize;
    auto barrier = GC_NEW(TaskBarrier);

    uint32_t* max_reachable = (uint32_t*)GC_MALLOC(sizeof(uint32_t) * threadCount);
    {
        uint32_t i;
        for (i = 0; i < totalWorkLoad % threadCount; ++i) {
            max_reachable[i] = workLoad + 1;
        }
        if (tail) {
            max_reachable[i++] = workLoad + 1;
        }
        for (; i < threadCount; ++i) {
            max_reachable[i] = workLoad;
        }
    }
    barrier->TaskBarrier::TaskBarrier(threadCount, max_reachable, ret);
    uint16_t loopTaskInsertIndex = 0;

    for (auto i = start; i + blockSize <= count + start; i += blockSize, loopTaskInsertIndex = (loopTaskInsertIndex + 1) % threadCount) {
        //printf("Start task from %lu to %lu\n", i, i + blockSize);
        auto t = gcnewLoopTask(body, args, i, blockSize, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
    if (tail) {
        //printf("Start task from %lu to %lu\n", start + count - tail, start + count);
        auto t = gcnewLoopTask(body, args, start + count - tail, tail, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
    return ret;
}

void* copyArgs(void* mutableArgs, uint32_t argSize) {
    auto ret = GC_MALLOC(argSize);
    memcpy(ret, mutableArgs, argSize);
    return ret;
}

FB_INTERNAL(void)* runLoopCoroutineAsync(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize)
{
    void* ret = nullptr;
    runLoopCoroutineAsyncTask(body, args, mutableArgs, mutableArgSize, start, count, blockSize, &ret);
    return ret;
}

FB_INTERNAL(void) runLoopCoroutineAsyncTask(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize, void** _ret)
{
    auto ret = gcnewTask(nullptr, nullptr, false);
    *_ret = ret;
    auto totalWorkLoad = count / blockSize;
    auto pool = getThreadPool(); // threadCount is available only after the first getThreadPool() call
    auto workLoad = totalWorkLoad / threadCount;
    auto tail = count % blockSize;
    auto barrier = GC_NEW(TaskBarrier);

    uint32_t* max_reachable = (uint32_t*)GC_MALLOC(sizeof(uint32_t) * threadCount);
    {
        uint32_t i;
        for (i = 0; i < totalWorkLoad % threadCount; ++i) {
            max_reachable[i] = workLoad + 1;
        }
        if (tail) {
            max_reachable[i++] = workLoad + 1;
        }
        for (; i < threadCount; ++i) {
            max_reachable[i] = workLoad;
        }
    }
    barrier->TaskBarrier::TaskBarrier(threadCount, max_reachable, ret);
    uint16_t loopTaskInsertIndex = 0;

    for (auto i = start; i + blockSize <= count + start; i += blockSize, loopTaskInsertIndex = (loopTaskInsertIndex + 1) % threadCount) {
        //printf("Start task from %lu to %lu\n", i, i + blockSize);
        auto t = gcnewMutableLoopTask(body, args, copyArgs(mutableArgs, mutableArgSize), i, blockSize, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
    if (tail) {
        //printf("Start task from %lu to %lu\n", start + count - tail, start + count);
        auto t = gcnewMutableLoopTask(body, args, copyArgs(mutableArgs, mutableArgSize), start + count - tail, tail, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
}

FB_INTERNAL(void)* runLoopCoroutineAsyncOffs(bool(*body)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint32_t mutableArgSize, uint64_t start, uint64_t count, uint64_t blockSize, uint32_t mutThisTaskOffset)
{
    auto ret = gcnewTask(nullptr, nullptr, false);

    auto totalWorkLoad = count / blockSize;
    auto pool = getThreadPool(); // threadCount is available only after the first getThreadPool() call
    auto workLoad = totalWorkLoad / threadCount;
    auto tail = count % blockSize;
    auto barrier = GC_NEW(TaskBarrier);

    uint32_t* max_reachable = (uint32_t*)GC_MALLOC(sizeof(uint32_t) * threadCount);
    {
        uint32_t i;
        for (i = 0; i < totalWorkLoad % threadCount; ++i) {
            max_reachable[i] = workLoad + 1;
        }
        if (tail) {
            max_reachable[i++] = workLoad + 1;
        }
        for (; i < threadCount; ++i) {
            max_reachable[i] = workLoad;
        }
    }
    barrier->TaskBarrier::TaskBarrier(threadCount, max_reachable, ret);
    uint16_t loopTaskInsertIndex = 0;
    //std::vector<uint32_t> tasksPerThread(threadCount);

    for (auto i = start; i + blockSize <= count + start; i += blockSize, loopTaskInsertIndex = (loopTaskInsertIndex + 1) % threadCount) {
        //printf("Start task from %llu to %llu on thread %u\n", i, i + blockSize, loopTaskInsertIndex);
        auto mutArgs = copyArgs(mutableArgs, mutableArgSize);
        auto t = gcnewMutableLoopTask(body, args, mutArgs, i, blockSize, barrier);
        t->thread = loopTaskInsertIndex;
        *((volatile void**)(((char*)mutArgs) + mutThisTaskOffset)) = t;
        pool[loopTaskInsertIndex].enqueue(t);
        //tasksPerThread[loopTaskInsertIndex]++;
    }
    if (tail) {
        //printf("Start task from %llu to %llu\n", start + count - tail, start + count);
        auto mutArgs = copyArgs(mutableArgs, mutableArgSize);
        auto t = gcnewMutableLoopTask(body, args, mutArgs, start + count - tail, tail, barrier);
        t->thread = loopTaskInsertIndex;
        *((volatile void**)(((char*)mutArgs) + mutThisTaskOffset)) = t;
        pool[loopTaskInsertIndex].enqueue(t);
        //tasksPerThread[loopTaskInsertIndex]++;
    }

    /*for (uint16_t i = 0; i < threadCount; ++i) {
        if (max_reachable[i] != tasksPerThread[i]) {
            printf("ERROR: invalid thread-scheduling\n");
        }
    }*/

    return ret;
}

FB_INTERNAL(void)* reduceAsync(void* (*body)(char*, uint64_t, uint64_t), void* (*reducer)(void*, void*), char* values, uint32_t valuec, uint64_t blockSize, void* seed)
{
    auto ret = gcnewTaskT(nullptr, nullptr, false, nullptr);
    auto totalWorkLoad = valuec / blockSize;
    auto pool = getThreadPool(); // threadCount is available only after the first getThreadPool() call
    auto workLoad = totalWorkLoad / threadCount;
    auto tail = valuec % blockSize;
    auto barrier = GC_NEW(ReduceBarrier);

    uint32_t* max_reachable = (uint32_t*)GC_MALLOC(sizeof(uint32_t) * threadCount);
    {
        uint32_t i;
        for (i = 0; i < totalWorkLoad % threadCount; ++i) {
            max_reachable[i] = workLoad + 1;
        }
        if (tail) {
            max_reachable[i++] = workLoad + 1;
        }
        for (; i < threadCount; ++i) {
            max_reachable[i] = workLoad;
        }
    }
    new(barrier) ReduceBarrier(threadCount, max_reachable, ret, reducer, seed);
    uint16_t loopTaskInsertIndex = 0;

    for (auto i = 0; i + blockSize <= valuec; i += blockSize, loopTaskInsertIndex = (loopTaskInsertIndex + 1) % threadCount) {
        //printf("Start task from %lu to %lu\n", i, i + blockSize);
        auto t = gcnewReducingLoopTask(body, values, i, blockSize, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
    if (tail) {
        //printf("Start task from %lu to %lu\n", start + count - tail, start + count);
        auto t = //gcnewLoopTask(body, args, start + count - tail, tail, barrier);
            gcnewReducingLoopTask(body, values, valuec - tail, tail, barrier);
        t->thread = loopTaskInsertIndex;
        pool[loopTaskInsertIndex].enqueue(t);
    }
    return ret;
}

FB_INTERNAL(void)* getCompletedTask()
{
    static auto ret = gcnewTask(nullptr, nullptr, true);
    return ret;
}

FB_INTERNAL(void)* getCompletedTaskT(void* result)
{
    return gcnewTaskT(nullptr, nullptr, true, result);
}

FB_INTERNAL(void)* waitTaskValue(void* taskt)
{
    if (!taskt)
        return nullptr;
    auto t = reinterpret_cast<TaskT>(taskt);
    if (t->ty == ValueTask)
        return value_event_wait(&t->complete);
    else if (t->ty == ActorValueTaskTy) {
        auto avt = (ActorValueTask)t;
        return value_event_wait(&avt->task.complete);
    }
    return nullptr;
}

FB_INTERNAL(void)* getTaskValue(void* taskt)
{
    if (!taskt)
        return nullptr;
    auto t = (TaskT)taskt;
    if (t->ty == ValueTask)
        return t->complete.value.load(std::memory_order_acquire).value;
    else if (t->ty == ActorValueTaskTy) {
        auto avt = (ActorValueTask)t;
        return avt->task.complete.value.load(std::memory_order_acquire).value;
    }
    return nullptr;
}

FB_INTERNAL(void) waitTask(void* task)
{
    if (task) {
        auto t = reinterpret_cast<Task>(task);

        printf("wait for task %p\n", task);
        if (t->ty == VoidTask)
            event_wait(&t->complete);
        else if (t->ty == ActorVoidTaskTy) {
            auto avt = reinterpret_cast<ActorVoidTask>(t);
            event_wait(&avt->task.complete);
        }
        else {
            printf("Error: no voidTask\n");
        }
        printf("waiting for task %p completed\n", task);
    }
}

FB_INTERNAL(bool) isTaskCompleted(void* task)
{
    auto tt = (TaskType*)task;
    if (*tt == VoidTask) {
        auto t = (Task)task;
        return t->complete.value.load(std::memory_order_acquire);
    }
    else if (*tt == ValueTask) {
        auto t = (TaskT)task;
        return t->complete.value.load(std::memory_order_acquire).hasValue;
    }
    return false;
}

FB_INTERNAL(bool) isTask(void* task)
{
    if (task == nullptr)
        return false;
    auto t = (Task)task;
    switch (t->ty) {
        case VoidTask: {
            return t->args != nullptr && t->coro_resume != nullptr;
        }
        case ValueTask: {
            auto vt = (TaskT)t;
            return vt->args != nullptr && vt->coro_resume != nullptr;
        }
        case LoopTaskTy: {
            auto lt = (LoopTask)t;
            return lt->args != nullptr && lt->barrier != nullptr && lt->coro_resume != nullptr && lt->count != 0 && lt->thread < threadCount;
        }
        case MutableLoopTaskTy: {
            auto mlt = (MutableLoopTask)t;

            return mlt->immutableArgs != nullptr && mlt->mutableArgs != nullptr && mlt->barrier != nullptr && mlt->coro_resume != nullptr && mlt->count != 0 && mlt->thread < threadCount;

        }
        case ActorTaskTy: {
            auto at = (ActorTask)t;
            return at->ctx != nullptr;
        }
        case ActorVoidTaskTy: {
            auto avt = (ActorVoidTask)t;
            return avt->ctx != nullptr && isTask(&avt->task);
        }
        case ActorValueTaskTy: {
            auto avt = (ActorValueTask)t;
            return avt->ctx != nullptr && isTask(&avt->task);
        }
        default:
            return false;
    }
}

uint16_t getCurrentThreadID()
{
    return threadID;
}

bool awaiterEnqueue(std::atomic<awaiter_t*> * head, Task aw) {

    auto item = gc_newT<awaiter_t>();
    item->t = aw;
    auto oldValue = head->load(std::memory_order_acquire);
    auto finishedAwaiter = getFinishedAwaiter();
    do {
        if (oldValue == finishedAwaiter)
            return false;
        item->next = oldValue;
    } while (oldValue != finishedAwaiter && !head->compare_exchange_weak(oldValue, item, std::memory_order_release, std::memory_order_acquire));

    if (oldValue == item) {
        printf("ERROR: Task %p awaites itself\n", oldValue);
    }
    item->next = oldValue;
    return oldValue != finishedAwaiter;
    /*auto oldValue = head->exchange(item, std::memory_order_acq_rel);
    item->next = oldValue;
    auto finishedAwaiter = getFinishedAwaiter();
    if (oldValue == finishedAwaiter) {
        item->t = nullptr;
        // schedule tasks which are enqueued in the mean-time, where head is item instead of finishedAwaiter
        scheduleAwaiters(head, nullptr);
        return false;
    }
    return true;*/

}

FB_INTERNAL(bool) taskAwaiterEnqueue(Task t, Task _awaiter)
{
    //static uint32_t counter = 0;
    if (_awaiter == nullptr) {
        printf("ERROR: await null from %s\n", t != nullptr ? taskType_name(t->ty) : "null");
        return false;
    }
    if (t == nullptr) {
        printf("ERROR: await %s from null\n", taskType_name(_awaiter->ty));
        return false;
    }

    //printf("#%u: %s %p waits for %s %p\n", ++counter, taskType_name(_awaiter->ty), _awaiter, taskType_name(t->ty), t);
    switch (t->ty) {
        case VoidTask:
            return awaiterEnqueue(&t->awaiters, _awaiter);
        case ValueTask: {
            auto _t = reinterpret_cast<TaskT>(t);
            return awaiterEnqueue(&_t->awaiters, _awaiter);
        }
        case ActorVoidTaskTy: {
            auto avt = reinterpret_cast<ActorVoidTask>(t);
            return awaiterEnqueue(&avt->task.awaiters, _awaiter);
        }
        case ActorValueTaskTy: {
            auto avt = reinterpret_cast<ActorValueTask>(t);
            return awaiterEnqueue(&avt->task.awaiters, _awaiter);
        }
        default:
            printf("Awaiter-enqueue failed for await %s from %s\n", taskType_name(t->ty), _awaiter == nullptr ? "NULL" : taskType_name(_awaiter->ty));
            return false;
    }

}