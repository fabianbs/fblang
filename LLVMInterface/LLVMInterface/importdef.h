/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#ifdef LLVM_Interface_Export_Symbols
#define EXTERN_API(x) extern "C" x __declspec(dllexport)
#else
#define EXTERN_API(x) extern "C" x __declspec(dllimport)
#endif
