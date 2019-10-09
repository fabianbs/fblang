/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "AsyncFileIO.h"
#include <Windows.h>
#include "GC_NEW.h"
#include "AsyncIO.h"

struct AwaitableOverlapped {
	OVERLAPPED ov;
	_TaskT task;
};

FB_INTERNAL(void)* createAsyncFile(char * filename, uint32_t fnLength, FileMode mode)
{
	DWORD rw = 0, mod = 0;

	switch (mode) {
		case FileMode::CreateNew:
			rw = GENERIC_WRITE;
			mod = CREATE_ALWAYS;
			break;
		case FileMode::AppendOrCreate:
			rw = FILE_APPEND_DATA | GENERIC_READ;
			mod = OPEN_ALWAYS;
			break;
		case FileMode::Open:
			rw = GENERIC_READ;
			mod = OPEN_EXISTING;
			break;
		case FileMode::OpenOrCreate:
			rw = GENERIC_READ | GENERIC_WRITE;
			mod = OPEN_ALWAYS;
			break;
	}
	HANDLE ret = ::CreateFileA(
		std::string(filename, fnLength).c_str(),
		rw,
		0,
		0,
		mod,
		FILE_FLAG_OVERLAPPED,
		NULL);
	HANDLE iocp = getIOCompletionPort();
	CreateIoCompletionPort(ret, iocp, 0, 0);
	return ret;
}

FB_INTERNAL(_TaskT)* readFileAsync(void * hFile, char * buffer, uint32_t buffersize, uint64_t offset)
{
	auto ov = gc_newT<AwaitableOverlapped>();
	ov->ov.Offset = (DWORD)offset;
	ov->ov.OffsetHigh = (DWORD)(offset >> 32);
	DWORD num;
	auto succ = ReadFile(hFile, buffer, buffersize, &num, &ov->ov);
	if (!succ && GetLastError() != ERROR_IO_PENDING) {
		num = ~0;
	}
	TaskT task = &ov->task;
	task->complete = Optional(succ, (void*)(size_t)num);
	task->coro_resume = nullptr;
	return task;
}

FB_INTERNAL(_TaskT)* writeFileAsync(void * hFile, char * buffer, uint32_t buffersize, uint64_t offset)
{
	auto ov = gc_newT<AwaitableOverlapped>();
	ov->ov.Offset = (DWORD)offset;
	ov->ov.OffsetHigh = (DWORD)(offset >> 32);
	DWORD num;
	auto succ = WriteFile(hFile, buffer, buffersize, &num, &ov->ov);
	if (!succ && GetLastError() != ERROR_IO_PENDING) {
		num = ~0;
	}
	TaskT task = &ov->task;
	task->complete = Optional(succ, (void*)(size_t)num);
	task->coro_resume = nullptr;
	return task;
}

FB_INTERNAL(void) closeAsyncFile(void * hFile)
{
	if (hFile) {
		CloseHandle(hFile);
	}
}
