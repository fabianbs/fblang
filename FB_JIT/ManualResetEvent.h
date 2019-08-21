/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <atomic>
#include <iostream>
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
typedef struct _ManualResetEvent {
	std::atomic<int> value;
	_ManualResetEvent& operator =(int val) {
		value.store(val, std::memory_order_release);
		return *this;
	}
}ManualResetEvent;
typedef struct _Optional {
	int hasValue;
	char pad[4];
	void* value;
	_Optional() {
		memset(this, 0, sizeof(Optional));
	}
	_Optional(int has, void* val) {
		memset(this, 0, sizeof(Optional));
		hasValue = has;
		value = val;
	}
    friend std::ostream& operator<<(std::ostream& os, const _Optional& op);
}Optional;
typedef struct _ValueResetEvent {
	std::atomic<Optional> value;
	_ValueResetEvent():value(Optional()) {
		
	}
	_ValueResetEvent& operator=(Optional&& op) {
		value.store(op, std::memory_order_release);
		return *this;
	}
	_ValueResetEvent& operator=(const Optional& op) {
		value.store(op, std::memory_order_release);
		return *this;
	}
}ValueResetEvent;

FB_INTERNAL(bool) event_set(ManualResetEvent * evt);
FB_INTERNAL(void) event_wait(ManualResetEvent * evt);
FB_INTERNAL(void) event_wait_millis(ManualResetEvent * evt, uint32_t millis);
FB_INTERNAL(void) event_reset(ManualResetEvent * evt);
FB_INTERNAL(bool) event_isset(ManualResetEvent * evt);

FB_INTERNAL(bool) value_event_set(ValueResetEvent *evt, void* value);
FB_INTERNAL(void)* value_event_wait(ValueResetEvent *evt);
FB_INTERNAL(bool) value_event_wait_millis(ValueResetEvent *evt, uint32_t millis, void** result);
FB_INTERNAL(void) value_event_reset(ValueResetEvent *evt);
FB_INTERNAL(bool) value_event_isset(ValueResetEvent *evt, void** result);


