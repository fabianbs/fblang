/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include"Task.h"
typedef struct _Behavior {
	void(*coro_resume)(void*);
	void* args;
}Behavior;
typedef struct _ActorQNode {
	Behavior be;
	std::atomic<_ActorQNode *> next;
}*ActorQNode;
typedef struct _Actor
{
	std::atomic<ActorQNode> head;
	ActorQNode tail;
	std::atomic<void*> thread_id;
}*Actor;

#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

FB_INTERNAL(void) actorSched(Actor ac, void(*coro_resume)(void*), void* args);
Behavior actorNextBe(Actor ac);
bool actorEmpty(Actor ac);
