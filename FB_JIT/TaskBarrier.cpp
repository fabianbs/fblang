#include "TaskBarrier.h"

#include <gc.h>
#include <cassert>
#include <ostream>
#include "TaskScheduler.h"
TaskBarrier::TaskBarrier(uint16_t _numThreads, uint32_t* _maxReachable, Task  _task)
    :maxReachable(_maxReachable), numThreads(_numThreads), task(_task), counter(0)
{
    reached = (uint32_t*)GC_MALLOC(numThreads * sizeof(uint32_t));
    memset(reached, 0, numThreads * sizeof(uint32_t));
    maxCounter = 0;
    for (uint16_t i = 0; i < numThreads; ++i) {
        if (maxReachable[i])
            maxCounter++;
    }
    if (!maxCounter) {
        finalizeTask(task, nullptr);
    }
}

void TaskBarrier::reach(uint16_t id)
{
    //static std::atomic_uint count = 0;
    if (id != getCurrentThreadID()) {
        printf("ERROR: reach barrier from thread %d instead of thread %d\n", id, getCurrentThreadID());
    }
    //printf("Reach barrier for task %d: %u/%u\n", id, reached[id] + 1, maxReachable[id]);

    assert(this != nullptr);
    assert(id < numThreads);
    if (++reached[id] == maxReachable[id]) {
        auto num = 1 + counter.fetch_add(1, std::memory_order_acq_rel);
        //printf(">> thread %d finished\n", id);
        if (num == maxCounter) {
            finalizeTask(task, nullptr);
        }
    }
    else if (reached[id] > maxReachable[id]) {
        printf("Reach barrier from thread %d %d/%d\n", id, reached[id], maxReachable[id]);
    }
    /*if (count.load() == 25) {
        printf("");
    }*/
}

void TaskBarrier::reach(uint16_t id) volatile
{
    if (id != getCurrentThreadID()) {
        printf("ERROR: reach barrier from thread %d instead of thread %d\n", id, getCurrentThreadID());
    }
    //printf("Reach barrier for task %d: %u/%u\n", id, reached[id] + 1, maxReachable[id]);

    assert(this != nullptr);
    assert(id < numThreads);
    if (++reached[id] == maxReachable[id]) {
        auto num = 1 + counter.fetch_add(1, std::memory_order_acq_rel);
        //printf(">> thread %d finished\n", id);
        if (num == maxCounter) {
            finalizeTask(task, nullptr);
        }
    }
}

std::ostream& operator<<(std::ostream& os, const TaskBarrier& barrier)
{
    os << "TaskBarrier{numThreads: " << barrier.numThreads << ", counter: " << barrier.counter << "/" << barrier.maxCounter << ", reached: [";
    for (uint16_t i = 0; i < barrier.numThreads; ++i) {
        os << barrier.reached[i] << "/" << barrier.maxReachable[i];
        if (i < barrier.numThreads - 1) {
            os << ", ";
        }
    }
    os << "]}";
    return os;
}

ReduceBarrier::ReduceBarrier(uint16_t _numThreads, uint32_t* _maxReachable, TaskT _task, void* (*_reducer)(void*, void*), void* seed)
    :maxReachable(_maxReachable), numThreads(_numThreads), task(_task), counter(0), reducer(_reducer)
{

    reached = (aligned_uint32_t*)GC_memalign(CACHE_LINE_SIZE, numThreads * sizeof(aligned_uint32_t));
    memset(reached, 0, numThreads * sizeof(uint32_t));
    maxCounter = 0;
    
    values = (aligned_pvoid_t*)GC_memalign(CACHE_LINE_SIZE, numThreads * sizeof(aligned_pvoid_t));
    for (uint16_t i = 0; i < numThreads; ++i) {
        if (maxReachable[i])
            maxCounter++;
        values[i] = seed;
    }
    if (!maxCounter) {
        finalizeTaskT(task, seed, nullptr);
    }
}

void ReduceBarrier::reach(uint16_t id, void* value)
{
    if (id != getCurrentThreadID()) {
        printf("ERROR: reach barrier from thread %d instead of thread %d\n", id, getCurrentThreadID());
    }
    //printf("Reach barrier with value %d\n", (int)value);

    assert(this != nullptr);
    assert(id < numThreads);
    values[id] = reducer(values[id], value);
    if (maxReachable[id] == ++reached[id]) {
        
        //printf(">> thread %d finished\n", id);

        assert(numThreads > 0);
        
        auto num = 1 + counter.fetch_add(1, std::memory_order_acq_rel);
        if (num == maxCounter) {
            void* result = values[0];
            for (uint16_t i = 1; i < numThreads; ++i) {
                if (maxReachable[i]) {
                    result = reducer(result, values[i]);
                }
            }
            finalizeTaskT(task, result, nullptr);
        }
    }
    else if (static_cast<uint32_t>(reached[id]) > maxReachable[id]) {
        printf("Reach barrier from thread %d %d/%d\n", id, (uint32_t)reached[id], maxReachable[id]);
    }
}
