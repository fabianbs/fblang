/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "AsyncIO.h"
#include <Windows.h>
#include <thread>
#include "Task.h"
#include "GC_NEW.h"
#include "ManualResetEvent.h"
#include "TaskScheduler.h"
struct AwaitableOverlapped {
	OVERLAPPED ov;
	_TaskT task;
};

volatile HANDLE iocp = NULL;

std::thread *iothreads;

void IOProc(void* iocp) {
	while (true) {
		AwaitableOverlapped * awo = nullptr;
		DWORD numBytes = 0;
		ULONG_PTR ky = 0;
		BOOL unused = GetQueuedCompletionStatus(iocp, &numBytes, &ky, (LPOVERLAPPED*)&awo, INFINITE);

		//awo->task.result.store((void*)numBytes, std::memory_order_release);
		value_event_set(&awo->task.complete, (void*)(ptrdiff_t)numBytes);
		//event_set(&awo->task.task.complete);
		awaiter_t* nod = awo->task.awaiters.load(std::memory_order_acquire);
		auto pool = getThreadPool();
		while (nod != nullptr) {
			//nod->coro_resume(nod->args);
			runAsyncTask(nod->t, pool);
			nod = nod->next;
		}
	}
}


void * getIOCompletionPort()
{
	if (iocp == NULL) {
		iocp = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
		int count;
		iothreads = gc_newT<std::thread>(count = std::thread::hardware_concurrency());
		for (int i = 0; i < count; ++i) {
			iothreads[i].thread::thread(&IOProc, iocp);
		}
	}
	return iocp;
}
