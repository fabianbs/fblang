#include "SchedulerQueue.h"
#include <gc.h>

#define var auto

SchedulerQueue gcnewSchedQ()
{
	auto ret = (SchedulerQueue)GC_MALLOC(sizeof(_SchedulerQueue));
	initSchedQ(ret);
	return ret;
}

void initSchedQ(SchedulerQueue ret)
{
	auto dummy = gcnewSchedQNod(nullptr);
	dummy->isTail = true;
	ret->head = dummy;
	ret->tail = { dummy, 0 };
}

SchedulerQueueNode gcnewSchedQNod(Actor task)
{
	auto ret = (SchedulerQueueNode)GC_MALLOC(sizeof(_SchedulerQueueNode));
	ret->next = nullptr;
	ret->task = task;
	return ret;
}

bool schedQEnqueue(SchedulerQueue q, Actor nw)
{
	var nod = (SchedulerQueueNode)GC_MALLOC(sizeof(_SchedulerQueueNode));
	std::atomic_thread_fence(std::memory_order_release);

	var prev = std::atomic_exchange_explicit(&q->head, nod, std::memory_order_relaxed);
	auto ret = prev->isTail;
	std::atomic_store_explicit(&prev->next, nod, std::memory_order_relaxed);
	return ret;
}

Actor schedQDequeue(SchedulerQueue q)
{
	SchedulerQueueNode tail, next;
	SchedNodeABA cmp = q->tail.load(std::memory_order_relaxed);
	SchedNodeABA xchg;
	do {
		tail = cmp.node;
		next = //tail->next.load(std::memory_order_relaxed);
			std::atomic_load_explicit(&tail->next, std::memory_order_relaxed);
		if (next == nullptr)
			return nullptr;

		xchg.node = next;
		xchg.aba = cmp.aba + 1;

	} while (!std::atomic_compare_exchange_weak_explicit(&q->tail, &cmp, xchg, std::memory_order_relaxed, std::memory_order_relaxed));

	std::atomic_thread_fence(std::memory_order_acq_rel);

	auto ret = std::atomic_load_explicit(&next->task, std::memory_order_relaxed);

	//store null in next.task st. all following dequeues on this node will fail
	std::atomic_store_explicit(&next->task, (Actor)nullptr, std::memory_order_relaxed);
	//ensure the null-write is done before releasing memory
	while (std::atomic_load_explicit(&next->task, std::memory_order_relaxed) != nullptr) {}

	std::atomic_thread_fence(std::memory_order_acquire);
	GC_FREE(tail);
	return ret;
}


