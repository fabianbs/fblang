#pragma once
#include <atomic>
#include "GC_NEW.h"
#include <deque>
#include <mutex>
template<typename T>
struct mpsc_node {
    std::atomic<mpsc_node<T>*> next;
    T value;
};
// see "https://github.com/mstump/queues/blob/master/include/mpsc-queue.hpp"
template<typename T>
struct mpsc_queue {
    typedef typename std::aligned_storage<sizeof(mpsc_node<T>), std::alignment_of<mpsc_node<T>>::value>::type node_aligned_t;

    std::atomic<mpsc_node<T>*>head, tail;


    mpsc_queue() :
        head(reinterpret_cast<mpsc_node<T>*>(gc_newT< node_aligned_t>())),
        tail(head.load(std::memory_order_relaxed))
    {

        mpsc_node<T>* front = head.load(std::memory_order_relaxed);
        front->next.store(nullptr, std::memory_order_relaxed);

    }
};
template<typename T>
bool mpsc_enqueue(mpsc_queue<T>* q, T val) {
    mpsc_node<T>* node = reinterpret_cast<mpsc_node<T>*>(gc_newT< mpsc_queue<T>::node_aligned_t>());
    node->value = val;
    node->next.store(nullptr, std::memory_order_relaxed);

    mpsc_node<T>* prev_head = q->head.exchange(node, std::memory_order_acq_rel);
    prev_head->next.store(node, std::memory_order_release);
    return prev_head == q->tail.load(std::memory_order_relaxed);
}
template<typename T>
bool mpsc_empty(mpsc_queue<T>* q) {
    mpsc_node<T>* tail = q->tail.load(std::memory_order_relaxed);
    mpsc_node<T>* next = tail->next.load(std::memory_order_acquire);

    return next == nullptr;
}
template<typename T>
bool mpsc_dequeue(mpsc_queue<T>* q, T* ret) {
    mpsc_node<T>* tail = q->tail.load(std::memory_order_relaxed);
    mpsc_node<T>* next = tail->next.load(std::memory_order_acquire);

    if (next == nullptr) {
        return false;
    }

    *ret = next->value;
    q->tail.store(next, std::memory_order_release);
    GC_FREE(tail);
    return true;
}

/*template<typename T>struct mpsc_queue {
    std::deque<T> q;
    std::mutex m;
    mpsc_queue()
        :q(), m() {

    }
};
template<typename T>
bool mpsc_enqueue(mpsc_queue<T>* q, T val) {
    std::lock_guard<std::mutex> lck(q->m);
    bool previouslyEmpty = q->q.empty();
    q->q.push_back(val);
    return previouslyEmpty;
}

template<typename T>
bool mpsc_empty(mpsc_queue<T>* q) {
    std::lock_guard<std::mutex> lck(q->m);
    return q->q.empty();
}

template<typename T>
bool mpsc_dequeue(mpsc_queue<T>* q, T* ret) {
    std::lock_guard<std::mutex> lck(q->m);
    if (q->q.empty())
        return false;
    *ret = q->q.front();
    q->q.pop_front();
    return true;
}
*/