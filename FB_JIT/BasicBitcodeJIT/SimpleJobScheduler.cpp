/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "SimpleJobScheduler.h"
#include "SchedulerQueue.h"
#include <thread>
#include <gc.h>
#include "ManualResetEvent.h"

struct SimpleSched;

std::atomic< SimpleSched *> schedQs = nullptr;
volatile uint32_t schedQc;
thread_local uint32_t index = 0;

struct SimpleSched {
	std::thread m_thread;
private:
	_SchedulerQueue m_queue;
	ManualResetEvent active;
public:
	SimpleSched() {
		initSchedQ(&m_queue);
		m_thread = std::thread(&SimpleSched::operator(), this);
		active.value = 1;
	}
	void operator() () {
		while (true) {
			Actor ac = this->dequeue();
			if (ac == nullptr) {
				do {
					//steal
					auto q = schedQs.load(std::memory_order_relaxed);
					auto count = schedQc;
					for (uint32_t i = 0; i < count; ++i) {
						SimpleSched* sched = &q[i];
						if (sched != this) {
							ac = sched->dequeue();
							if (ac != nullptr)
								break;
						}
					}
					if (ac == nullptr) {
						event_reset(&active);
						event_wait(&active);
					}
				} while (ac == nullptr);
			}
			Behavior be = actorNextBe(ac);
			if (be.coro_resume) {
				be.coro_resume(be.args);
			}
			if (!actorEmpty(ac)) {
				// enqueue on same thread for cache locality
				schedQEnqueue(&m_queue, ac);
			}
			else {
				ac->thread_id.store(nullptr, std::memory_order_release);
			}
		}
	}
	void enqueue(Actor ac) {
		if (schedQEnqueue(&m_queue, ac)) {
			// event set nur, wenn vorher leer!
			event_set(&active);
		}
	}
	Actor dequeue() {
		return schedQDequeue(&m_queue);
	}
};


FB_INTERNAL(void) schedInit()
{
	schedInitThreads(std::thread::hardware_concurrency());
}


FB_INTERNAL(void) schedInitThreads(uint32_t threadc)
{
	schedQs = (SimpleSched*)GC_MALLOC(threadc * sizeof(SimpleSched));
	schedQc = threadc;
	for (uint32_t i = 0; i < threadc; ++i) {
		schedQs[i].SimpleSched::SimpleSched();
	}
}

FB_INTERNAL(void) schedActor(Actor ac)
{
	auto id = (SimpleSched*)ac->thread_id.load(std::memory_order_acquire);
	if (id == nullptr) {
		//overflow on adding is ok

		auto ind = index++ % schedQc;
		auto q = schedQs.load(std::memory_order_relaxed);
		if (q == nullptr) {
			schedInit();
			q = schedQs.load(std::memory_order_relaxed);
		}
		id = &q[ind];

		ac->thread_id.store(id, std::memory_order_release);
	}

	id->enqueue(ac);
}
