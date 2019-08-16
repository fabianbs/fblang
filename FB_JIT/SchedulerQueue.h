#pragma once
#include<atomic>
#include "Actor.h"

// vgl: pony scheduler
typedef struct _SchedulerQueueNode {
	std::atomic<Actor> task;
	std::atomic<_SchedulerQueueNode *>next;
	bool isTail = false;
}*SchedulerQueueNode;

typedef struct SchedNodeABA {
	struct
	{
		SchedulerQueueNode node;
		uintptr_t aba;
	};
};

typedef struct _SchedulerQueue
{
	std::atomic<SchedulerQueueNode> head;
	std::atomic<SchedNodeABA> tail;
}*SchedulerQueue;



SchedulerQueue gcnewSchedQ();
void initSchedQ(SchedulerQueue q);
SchedulerQueueNode gcnewSchedQNod(Actor task);
bool schedQEnqueue(SchedulerQueue q, Actor nw);
Actor schedQDequeue(SchedulerQueue q);