#include "Delay.h"
#include <thread>
#include <chrono>
#include <Windows.h>
#include "GC_NEW.h"
#include "Task.h"
#include "TaskScheduler.h"

FB_INTERNAL(void) thread_sleep(uint32_t millis)
{
    std::this_thread::sleep_for(std::chrono::milliseconds(millis));
}
HANDLE timerQ = nullptr;

struct TimerProcArg {
    void(*callback)(void*);
    void* args;
};

VOID CALLBACK TimerProc(PVOID lpParam, BOOLEAN TimerOrWaitFired) {
    if (lpParam != nullptr) {
        auto task = (Task)lpParam;
        finalizeTask(task, getThreadPool());
    }
}

FB_INTERNAL(_Task)* delay(uint32_t millis)
{
    if (timerQ == nullptr) {
        timerQ = CreateTimerQueue();
    }
    HANDLE timer;
    auto cb = gcnewTask(nullptr, nullptr, false);
    if (!CreateTimerQueueTimer(&timer, timerQ, &TimerProc, cb, millis, 0, 0)) {
        event_set(&cb->complete);
        // cb has not escaped from this method -> scheduleAwaiters is not necessary
    }
    return cb;
}
