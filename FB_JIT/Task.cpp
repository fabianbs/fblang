#include "Task.h"
#include <gc.h>
#include <algorithm>
#include <sstream>
#include "TaskBarrier.h"
#include "ManualResetEvent.h"

FB_INTERNAL(_Task)* gcnewTask(bool(*coro_resume)(void*), void* args, bool complete)
{
    auto ret = (Task)GC_MALLOC(sizeof(_Task));
    ret->ty = VoidTask;
    ret->coro_resume = coro_resume;
    ret->args = args;
    ret->cancelled = false;
    ret->awaiters.store(nullptr, std::memory_order_release);
    ret->complete = complete;

    return ret;
}

FB_INTERNAL(_TaskT)* gcnewTaskT(bool(*coro_resume)(void*, void**), void* args, bool complete, void* result)
{
    auto ret = (TaskT)GC_MALLOC(sizeof(_TaskT));
    ret->ty = ValueTask;
    ret->coro_resume = coro_resume;
    ret->args = args;
    ret->awaiters.std::atomic<awaiter_t*>::atomic(nullptr);
    Optional val;

    val.hasValue = complete;
    val.value = result;
    ret->complete.value.std::atomic<Optional>::atomic(val);
    ret->cancelled = false;
    return ret;
}
FB_INTERNAL(_LoopTask)* gcnewLoopTask(bool(*coro_resume)(void*, uint64_t, uint64_t), void* args, uint64_t start, uint64_t count, TaskBarrier* barrier)
{
    auto ret = GC_NEW(_LoopTask);
    ret->ty = TaskType::LoopTaskTy;
    ret->coro_resume = coro_resume;
    ret->args = args;
    ret->start = start;
    ret->count = count;
    ret->barrier = barrier;
    ret->cancelled = false;
    return ret;
}
FB_INTERNAL(_ReducingLoopTask)* gcnewReducingLoopTask(void* (*body)(char*, uint64_t, uint64_t), char* array, uint64_t start, uint64_t count, ReduceBarrier* barrier)
{
    auto ret = GC_NEW(_ReducingLoopTask);
    ret->ty = ReducingLoopTaskTy;
    ret->body = body;
    ret->array = array;
    ret->start = start;
    ret->count = count;
    ret->barrier = barrier;
    ret->cancelled = false;
    return ret;
}
FB_INTERNAL(_MutableLoopTask)* gcnewMutableLoopTask(bool(*coro_resume)(void*, void*, uint64_t*, uint64_t), void* args, void* mutableArgs, uint64_t start, uint64_t count, TaskBarrier* barrier)
{
    auto ret = GC_NEW(_MutableLoopTask);

    ret->ty = TaskType::MutableLoopTaskTy;
    ret->coro_resume = coro_resume;
    ret->immutableArgs = args;
    ret->mutableArgs = mutableArgs;
    ret->start = start;
    ret->count = count;
    ret->barrier = barrier;
    ret->cancelled = false;
    if (count == 0 || barrier == nullptr) {
        printf("Create invalid mutable loop task\n");
    }
    return ret;
}
FB_INTERNAL(_ActorTask)* gcnewActorTask(void* actorContext)
{
    auto ret = //GC_NEW(_ActorTask);
        gc_newT<_ActorTask>();
    ret->ty = ActorTaskTy;
    ret->ctx = reinterpret_cast<ActorContext>(actorContext);
    return ret;
}
FB_INTERNAL(_ActorVoidTask)* gcnewActorVoidTask(bool(*coro_resume)(void*), void* args, bool complete, void* actorContext)
{
    auto ret = GC_NEW(_ActorVoidTask);
    ret->ty = ActorVoidTaskTy;
    ret->task.ty = VoidTask;
    ret->task.coro_resume = coro_resume;
    ret->task.args = args;
    ret->task.cancelled = false;
    ret->task.awaiters.store(nullptr, std::memory_order_release);
    ret->task.complete = complete;
    ret->ctx = reinterpret_cast<ActorContext>(actorContext);
    return ret;
}
FB_INTERNAL(_ActorValueTask)* gcnewActorValueTask(bool(*coro_resume)(void*, void**), void* args, bool complete, void* result, void* actorContext)
{
    auto ret = GC_NEW(_ActorValueTask);
    ret->ty = ActorValueTaskTy;
    ret->task.ty = ValueTask;
    ret->task.coro_resume = coro_resume;
    ret->task.args = args;
    ret->task.awaiters.std::atomic<awaiter_t*>::atomic(nullptr);
    Optional val;

    val.hasValue = complete;
    val.value = result;
    ret->task.complete.value.std::atomic<Optional>::atomic(val);
    ret->task.cancelled = false;
    ret->ctx = reinterpret_cast<ActorContext>(actorContext);
    return ret;
}
FB_INTERNAL(void) initializeActorQueue(void** behavior_queue)
{
    auto q = reinterpret_cast<mpsc_queue<Task> * *>(behavior_queue);
    *q = GC_NEW(mpsc_queue<Task>);
    (*q)->mpsc_queue<Task>::mpsc_queue();
}
FB_INTERNAL(bool) completeTask(void* task)
{
    auto t = reinterpret_cast<Task>(task);
    if (t->ty == VoidTask && t->cancelled) {
        t->cancelled = false;
        return event_set(&t->complete);
    }
    return false;
}
FB_INTERNAL(bool) completeTaskT(void* taskt, void* result)
{
    auto t = reinterpret_cast<TaskT>(taskt);
    if (t->ty == ValueTask && t->cancelled) {
        t->cancelled = false;
        return value_event_set(&t->complete, result);
    }
    return false;
}


FB_INTERNAL(void) taskWait(Task t)
{
    event_wait(&t->complete);
}
awaiter_t* getFinishedAwaiter()
{
    static awaiter_t* ret = (awaiter_t*)GC_MALLOC(min(4ull, sizeof(awaiter_t)));
    return ret;
}
const char* taskType_name(TaskType ty)
{
    switch (ty) {
        case ErrorTask:return "ErrorTask";
        case VoidTask:return "VoidTask";
        case ValueTask:return "ValueTask";
        case LoopTaskTy:return "LoopTask";
        case MutableLoopTaskTy:return "MutableLoopTask";
        case ActorTaskTy:return "ActorTask";
        case ActorVoidTaskTy:return "ActorVoidTask";
        case ActorValueTaskTy:return "ActorValueTask";
        default:return "Unknown Task";
    }
}
std::string taskToString(Task t)
{
    if (t) {
        std::stringstream s;
        switch (t->ty) {
            case ErrorTask:
                return "Error-Task";
            case VoidTask:
                s << "VoidTask{args: " << t->args << ", awaiters: " << t->awaiters << ", cancelled: " << t->cancelled << ", complete: " << t->complete.value << ", coro_resume: " << t->coro_resume << "}";
                break;
            case ValueTask: {
                auto vt = (TaskT)t;
                s << "ValueTask{args: " << vt->args << ", awaiters: " << vt->awaiters << ", cancelled: " << vt->cancelled << ", complete: " << vt->complete.value << ", coro_resume: " << vt->coro_resume << "}";
                break;
            }
            case LoopTaskTy: {
                auto lt = (LoopTask)t;
                s << "LoopTask{args: " << lt->args << ", barrier: " << *lt->barrier << ", cancelled: " << lt->cancelled << ", coro_resume: " << lt->coro_resume << ", start: " << lt->start << ", count: " << lt->count << ", thread: " << lt->thread << "}";
                break;
            }
            case MutableLoopTaskTy: {
                auto lt = (MutableLoopTask)t;
                s << "MutableLoopTask{immutableArgs: " << lt->immutableArgs << ", mutableArgs: " << lt->mutableArgs << ", barrier: " << *lt->barrier << ", cancelled: " << lt->cancelled << ", coro_resume: " << lt->coro_resume << ", start: " << lt->start << ", count: " << lt->count << ", thread: " << lt->thread << "}";
                break;
            }
            case ActorTaskTy: {
                auto at = (ActorTask)t;
                s << "ActorTask{context: " << at->ctx << "}";
                break;
            }
            case ActorVoidTaskTy: {
                auto avt = (ActorVoidTask)t;
                s << "ActorVoidTask{context: " << avt->ctx << ", " << taskToString(&avt->task) << "}";
                break;
            }
            case ActorValueTaskTy: {
                auto avt = (ActorValueTask)t;
                s << "ActorVoidTask{context: " << avt->ctx << ", " << taskToString((Task)& avt->task) << "}";
                break;
            }
            default:
                return "Unknown Task";
        }
        return s.str();
    }
    else
        return "null";
}
FB_INTERNAL(void)* taskTWait(TaskT t) {
    return value_event_wait(&t->complete);
}
