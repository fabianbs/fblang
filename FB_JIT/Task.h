#pragma once
#include <atomic>
#include "MPSCQueue.h"
#include "ManualResetEvent.h"
#include <mutex>
#include "ActorContext.h"
#ifndef FB_INTERNALS
#define FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
enum TaskType {
    ErrorTask, 
    VoidTask, 
    ValueTask, 
    LoopTaskTy, 
    MutableLoopTaskTy,
    ActorTaskTy,
    ActorVoidTaskTy, 
    ActorValueTaskTy,
    ReducingLoopTaskTy
};
struct _Task;
class TaskBarrier;
class ReduceBarrier;

typedef struct _awaiter {
    _Task* t;
    _awaiter* next;
}awaiter_t;
typedef struct _Task {
    TaskType ty;
    bool(*coro_resume)(void*);
    void* args;
    bool cancelled;
    std::atomic<awaiter_t*> awaiters;
    ManualResetEvent complete;
}*Task;
typedef struct _TaskT {
    TaskType ty;
    bool(*coro_resume)(void*, void**);
    void* args;
    bool cancelled;
    std::atomic<awaiter_t*> awaiters;
    ValueResetEvent complete;

}*TaskT;
typedef struct _LoopTask {
    TaskType ty;
    bool(*coro_resume)(void*, uint64_t, uint64_t);
    void* args;
    uint64_t start;
    uint64_t count;
    bool cancelled;
    TaskBarrier* barrier;
    uint16_t thread;
}*LoopTask;
typedef struct _ReducingLoopTask {
    TaskType ty;
    void* (*body)(char*, uint64_t, uint64_t);
    char* array;
    uint64_t start;
    uint64_t count;
    bool cancelled;
    ReduceBarrier* barrier;
    uint16_t thread;
}*ReducingLoopTask;
typedef struct _MutableLoopTask {
    TaskType ty;
    //std::mutex m;
    bool(*coro_resume)(void*, void*, uint64_t*, uint64_t);
    void* immutableArgs, * mutableArgs;
    uint64_t start;
    uint64_t count;
    bool cancelled;
    TaskBarrier* barrier;
    uint16_t thread;

}*MutableLoopTask;

typedef struct _ActorTask {
    TaskType ty;
    ActorContext ctx;
}*ActorTask;
typedef struct _ActorVoidTask {
    TaskType ty;
    _Task task;
    ActorContext ctx;
}*ActorVoidTask;
typedef struct _ActorValueTask {
    TaskType ty;
    _TaskT task;
    ActorContext ctx;
}*ActorValueTask;

FB_INTERNAL(_Task)* gcnewTask(bool(*coro_resume)(void*), void* args, bool complete);
FB_INTERNAL(_TaskT)* gcnewTaskT(bool(*coro_resume)(void*, void**), void* args, bool complete, void* result);
FB_INTERNAL(_LoopTask)* gcnewLoopTask(bool(*coro_resume)(void*, uint64_t, uint64_t), void* args, uint64_t start, uint64_t count, TaskBarrier* barrier);
FB_INTERNAL(_ReducingLoopTask)* gcnewReducingLoopTask(void* (*body)(char*,uint64_t,uint64_t), char* array, uint64_t start, uint64_t count, ReduceBarrier* barrier);
FB_INTERNAL(_MutableLoopTask)* gcnewMutableLoopTask(bool(*coro_resume)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint64_t start, uint64_t count, TaskBarrier* barrier);
FB_INTERNAL(_ActorTask)* gcnewActorTask(void* actorContext);
FB_INTERNAL(_ActorVoidTask)* gcnewActorVoidTask(bool(*coro_resume)(void*), void* args, bool complete, void* actorContext);
FB_INTERNAL(_ActorValueTask)* gcnewActorValueTask(bool(*coro_resume)(void*, void**), void* args, bool complete, void* result, void* actorContext);

FB_INTERNAL(void) initializeActorQueue(void** behavior_queue);

FB_INTERNAL(bool) completeTask(void* task);
FB_INTERNAL(bool) completeTaskT(void* taskt, void* result);


FB_INTERNAL(void) taskWait(Task);
FB_INTERNAL(void)* taskTWait(TaskT);

awaiter_t* getFinishedAwaiter();
const char* taskType_name(TaskType ty);
std::string taskToString(Task t);