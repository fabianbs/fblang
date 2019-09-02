/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include "Structures.h"
#ifndef FB_INTERNALS
#define FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS


//stdio
FB_INTERNAL(void) cprintln(const char* str, size_t len);
FB_INTERNAL(void) cprintnwln();
FB_INTERNAL(void) cprint(const char* str, size_t len);
FB_INTERNAL(int) cprintf(const char* str, size_t len, ...);
FB_INTERNAL(int) cscanf(const char* str, size_t len, ...);
FB_INTERNAL(void) creadln(String*);
FB_INTERNAL(int32_t) creadky();


//string
FB_INTERNAL(void) to_str(int i, String*);
FB_INTERNAL(void) uto_str(uint32_t ui, String*);
FB_INTERNAL(void) zto_str(size_t z, String *);
FB_INTERNAL(void) lto_str(int64_t i, String*);
FB_INTERNAL(void) ulto_str(uint64_t ui, String*);
FB_INTERNAL(void) llto_str(int64_t hi, int64_t lo, String*);
FB_INTERNAL(void) dto_str(double d, String*);
FB_INTERNAL(void) fto_str(float f, String*);
FB_INTERNAL(void) span_to_str(Span s, String* ret);
FB_INTERNAL(int32_t) stoi(String str);
FB_INTERNAL(int64_t) stol(String str);
FB_INTERNAL(uint64_t) stoul(String str);
FB_INTERNAL(double) stof(String str);
FB_INTERNAL(void) strconcat(const char* str1, size_t len1, const char* str2, size_t len2, String* ret);
FB_INTERNAL(void) strmulticoncat(String * strs, size_t strc, String* ret);
FB_INTERNAL(void) strmul(const char* str, size_t str_len, uint32_t factor, String* ret);
FB_INTERNAL(void) stralloc(char c, size_t count, String*_ret);
FB_INTERNAL(int32_t) strcompare(const char* lhs, size_t lhs_len, const char* rhs, size_t rhs_len);
FB_INTERNAL(bool) strequals(const char* lhs, size_t lhs_len, const char* rhs, size_t rhs_len);
FB_INTERNAL(bool) isMatch(char* str_value, size_t str_length, char* pattern_value, size_t pattern_length);
FB_INTERNAL(void) strsplit_init(const char* str, size_t str_len, String* save);
FB_INTERNAL(bool) strsplit(const char* delims, size_t delims_len, String* outp, String* save);
FB_INTERNAL(void)* strsearch_init(const char* str, size_t str_len, const char* pattern, size_t pattern_len);
FB_INTERNAL(bool) strsearch(void* ctx, String* match);
FB_INTERNAL(void)* strscan_init_pattern(String* patterns, size_t patternc);
FB_INTERNAL(void)* strscan_init(const char* inp, size_t inp_len, void* pRegex);
FB_INTERNAL(bool) strscan(void* ctx, String* matchstr, uint32_t* groupID, uint32_t* pos);
FB_INTERNAL(int) string_scanf(char* strVal, size_t strLen, char* format, size_t formatLen, ...);
FB_INTERNAL(void) str_format(char* strVal, size_t strLen, String* ret, ...);

FB_INTERNAL(size_t) u8string_len(char* strVal, size_t strLen);
FB_INTERNAL(void)* u8string_it(char* strVal, size_t strLen);
FB_INTERNAL(bool) u8str_it_tryGetNext(void* it, uint32_t* cp);
FB_INTERNAL(void) u8str_codepoint_at(char* strVal, size_t strLen, size_t index, String* _ret);
//memory allocation
FB_INTERNAL(void) gc_init();
FB_INTERNAL(void)* gc_new(size_t size)noexcept(false);
FB_INTERNAL(void)* gc_new_with_dtor(size_t size, void(*finalizer)(void*, void*))noexcept(false);
FB_INTERNAL(void)* gc_resize_array(void* array, size_t newSize)noexcept(false);
//memory operations
FB_INTERNAL(Span) to_span(Array arr);
//file io
FB_INTERNAL(void)* openFile(String filename, FileMode mode);
FB_INTERNAL(void) closeFile(void* fileHandle);
FB_INTERNAL(uint32_t) writeFile(void* hFile, Span bytes);
FB_INTERNAL(uint32_t) readFile(void* hFile, Span buf);
FB_INTERNAL(uint64_t) getFileStreamPos(void* hFile);
FB_INTERNAL(bool) setfileStreamPos(void* hFile, int64_t pos, StreamPos relativeTo);

//math

FB_INTERNAL(bool) isPrime(uint64_t numb);

//sync

FB_INTERNAL(void)* gc_new_mutex();
FB_INTERNAL(void)* new_lock(void *mx);
FB_INTERNAL(void)* gc_new_cv();
FB_INTERNAL(void) unlock(void*lck);
FB_INTERNAL(void) waitFor(void* cv, void* lck);
FB_INTERNAL(bool) waitForMillis(void* cv, void* lck, uint64_t millis);
FB_INTERNAL(void) notify_one(void* cv);
FB_INTERNAL(void) notify_all(void* cv);

//sync_queues

FB_INTERNAL(void)* mpsc_enqueue(void** head, void* item);

// exceptions

FB_INTERNAL(void) throwException(const char* msg, uint32_t msgLen)noexcept(false);
FB_INTERNAL(void) throwOutOfBounds(uint32_t index, uint32_t length)noexcept(false);
FB_INTERNAL(void) throwOutOfBounds64(uint64_t index, uint64_t length)noexcept(false);
FB_INTERNAL(void) throwNullDereference(const char*msg, uint32_t msgLen)noexcept(false);
FB_INTERNAL(void) throwIfNull(void* ptr, const char* msg, uint32_t msgLen)noexcept(false);
FB_INTERNAL(void) throwIfOutOfBounds(uint32_t index, uint32_t len)noexcept(false);
FB_INTERNAL(void) throwIfOutOfBounds64(uint64_t index, uint64_t len)noexcept(false);
FB_INTERNAL(void) throwIfNullOrOutOfBounds(uint32_t index, uint32_t* len)noexcept(false);
FB_INTERNAL(void) throwIfNullOrOutOfBounds64(uint64_t index, uint64_t* len)noexcept(false);
FB_INTERNAL(void) initExceptionHandling();