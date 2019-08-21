/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "RandomNumberGenerator.h"
#include <random>


FB_INTERNAL(uint32_t) randomUInt()
{
    thread_local std::random_device rd;  //Will be used to obtain a seed for the random number engine
    thread_local std::mt19937 gen(rd()); //Standard mersenne_twister_engine seeded with rd()
    thread_local std::uniform_int_distribution<uint32_t> dis(0, UINT32_MAX);
    return dis(gen);
}
FB_INTERNAL(uint32_t) randomUIntRange(uint32_t minInclusive, uint32_t maxExclusive) {
    if (maxExclusive <= minInclusive)
        return minInclusive;
    return randomUInt() % (maxExclusive - minInclusive) + minInclusive;
}
FB_INTERNAL(int32_t) randomInt()
{
    thread_local std::random_device rd;  //Will be used to obtain a seed for the random number engine
    thread_local std::mt19937 gen(rd()); //Standard mersenne_twister_engine seeded with rd()
    thread_local std::uniform_int_distribution<int32_t> dis(INT32_MIN, INT32_MAX);
    return dis(gen);
}
FB_INTERNAL(int32_t) randomIntRange(int32_t minInclusive, int32_t maxExclusive) {
    if (maxExclusive <= minInclusive)
        return minInclusive;
    return randomUInt() % (maxExclusive - minInclusive) + minInclusive;
}
FB_INTERNAL(double) randomDouble() {
    thread_local std::random_device rd;  //Will be used to obtain a seed for the random number engine
    thread_local std::mt19937 gen(rd()); //Standard mersenne_twister_engine seeded with rd()
    thread_local std::uniform_real_distribution<double> dis(0, 1);
    return dis(gen);
}
FB_INTERNAL(double) randomDoubleRange(double start, double end) {
    if (end <= start)
        return start;
    return randomDouble() * (end - start) + start;
}