#include "ManualResetEvent.h"

# ifndef WIN32_LEAN_AND_MEAN
#  define WIN32_LEAN_AND_MEAN
# endif
#include <Windows.h>
#include <ostream>
// basics for event copied from https://github.com/lewissbaker/cppcoro/blob/master/lib/lightweight_manual_reset_event.cpp

std::ostream& operator<<(std::ostream& os, const _Optional& op)
{
    os << "Optional{hasValue: " << (bool)op.hasValue << ", value: " << op.value << "}";
    return os;
}

bool event_set(ManualResetEvent * evt)
{
	//evt->value.store(1, std::memory_order_release);
	int alreadySet = evt->value.exchange(1, std::memory_order_acq_rel);
	if (!alreadySet) {
		::WakeByAddressAll(&evt->value);
		return true;
	}
	return false;
}

void event_wait(ManualResetEvent * evt)
{
	event_wait_millis(evt, INFINITE);
}

void event_wait_millis(ManualResetEvent * evt, uint32_t millis)
{
	// Wait in a loop as WaitOnAddress() can have spurious wake-ups.
	int value = evt->value.load(std::memory_order_acquire);
	if (value)
		return;
	BOOL ok = TRUE;
	while (value == 0)
	{
		if (!ok)
		{
			// Previous call to WaitOnAddress() failed for some reason.
			// Put thread to sleep to avoid sitting in a busy loop if it keeps failing.
			::Sleep(1);
		}

		ok = ::WaitOnAddress(&evt->value, &value, sizeof(evt->value), millis);
		value = evt->value.load(std::memory_order_acquire);
	}
}

void event_reset(ManualResetEvent * evt)
{
	evt->value.store(0, std::memory_order_relaxed);
}

bool event_isset(ManualResetEvent * evt)
{
	return evt->value.load(std::memory_order_acquire);
}

FB_INTERNAL(bool) value_event_set(ValueResetEvent * evt, void * value)
{
	Optional exp;
	Optional nw (1, value);
	
	bool prev = evt->value.compare_exchange_strong(exp, nw, std::memory_order_acq_rel, std::memory_order_relaxed);
	if (prev) {
		::WakeByAddressAll(&evt->value);
		return true;
	}
	return false;

}

FB_INTERNAL(void)* value_event_wait(ValueResetEvent * evt)
{
	void* ret;
	if (value_event_wait_millis(evt, INFINITE, &ret))
		return ret;
	return nullptr;
}

FB_INTERNAL(bool) value_event_wait_millis(ValueResetEvent * evt, uint32_t millis, void ** result)
{
	// Wait in a loop as WaitOnAddress() can have spurious wake-ups.
	Optional value = evt->value.load(std::memory_order_acquire);
	if (value.hasValue)
	{
		*result = value.value;
		return true;
	}
	BOOL ok = TRUE;
	do
	{
		ok = ::WaitOnAddress(&evt->value, &value, sizeof(evt->value), millis);
		value = evt->value.load(std::memory_order_acquire);
		if (!ok)
		{
			// Previous call to WaitOnAddress() failed for some reason.
			// Put thread to sleep to avoid sitting in a busy loop if it keeps failing.
			::Sleep(1);
		}
	} while (value.hasValue == 0);
	*result = value.value;
	return value.hasValue;
}

FB_INTERNAL(void) value_event_reset(ValueResetEvent * evt)
{
	evt->value.store(Optional(), std::memory_order_relaxed);
}

FB_INTERNAL(bool) value_event_isset(ValueResetEvent * evt, void ** result)
{
	Optional val = evt->value.load(std::memory_order_acquire);
	if (val.hasValue) {
		*result = val.value;
	}
	else {
		*result = nullptr;
	}
	return val.hasValue;
}
