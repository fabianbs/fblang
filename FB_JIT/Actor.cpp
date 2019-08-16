#include "Actor.h"
#include "GC_NEW.h"
//vgl. pony actor messageq_t::push
FB_INTERNAL(void) actorSched(Actor ac, void(*coro_resume)(void*), void* args)
{
	auto nod = gc_newT<_ActorQNode>();
	nod->be = Behavior{ coro_resume, args };
	std::atomic_thread_fence(std::memory_order_release);

	auto prev = std::atomic_exchange_explicit(&ac->head, nod, std::memory_order_relaxed);
	if (prev != nullptr)
		std::atomic_store_explicit(&prev->next, nod, std::memory_order_relaxed);
	else
		ac->tail = nod;
}

Behavior actorNextBe(Actor ac)
{
	auto tail = ac->tail;
	auto nod = std::atomic_load_explicit(&tail->next, std::memory_order_relaxed);
	if (nod != nullptr) {
		ac->tail = nod;
		std::atomic_thread_fence(std::memory_order_acquire);
		GC_FREE(tail);
		return nod->be;
	}
	return Behavior{};
}

bool actorEmpty(Actor ac)
{
	return ac->tail->next.load(std::memory_order_acquire) == nullptr;
}
