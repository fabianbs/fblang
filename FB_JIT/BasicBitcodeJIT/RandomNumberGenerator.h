/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <cstdint>

#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
FB_INTERNAL(uint32_t) randomUInt();
FB_INTERNAL(uint32_t) randomUIntRange(uint32_t minInclusive, uint32_t maxExclusive);
FB_INTERNAL(int32_t) randomInt();
FB_INTERNAL(int32_t) randomIntRange(int32_t minInclusive, int32_t maxExclusive);
FB_INTERNAL(double) randomDouble();
FB_INTERNAL(double) randomDoubleRange(double start, double end);