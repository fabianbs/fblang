/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#ifndef FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS
#include <cstdint>
#include "Task.h"
#include "Structures.h"

FB_INTERNAL(void) * createAsyncFile(char* filename, uint32_t fnLength, FileMode mode);

FB_INTERNAL(_TaskT) * readFileAsync(void* hFile, char* buffer, uint32_t buffersize, uint64_t offset);

FB_INTERNAL(_TaskT) * writeFileAsync(void* hFile, char* buffer, uint32_t buffersize, uint64_t offset);

FB_INTERNAL(void) closeAsyncFile(void* hFile);
