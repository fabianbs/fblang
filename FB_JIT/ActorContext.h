#pragma once
#ifndef FB_INTERNALS
#define FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

#include "MPSCQueue.h"
#include <atomic>
struct _Task;
typedef struct _ActorContext {
    _Task* actorTask;
    mpsc_queue<_Task*> queue;
    uint16_t thread;
    std::atomic_bool isScheduled;
    _ActorContext();
}*ActorContext;
FB_INTERNAL(_ActorContext)* gcnewActorContext();
FB_INTERNAL(void) initActorContext(ActorContext* ctx);