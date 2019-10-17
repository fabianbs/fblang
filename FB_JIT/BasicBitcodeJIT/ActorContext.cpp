/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "ActorContext.h"
#include "Task.h"

FB_INTERNAL(_ActorContext)* gcnewActorContext()
{
    auto ret = GC_NEW(_ActorContext);
    new(ret)_ActorContext();
    return ret;
}

FB_INTERNAL(void) initActorContext(ActorContext* ctx)
{
    *ctx = GC_NEW(_ActorContext);
    new(*ctx)_ActorContext();
}

_ActorContext::_ActorContext()
    :queue(), isScheduled(false), thread(0), actorTask(nullptr)
{
}
