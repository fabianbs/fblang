/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <stdint.h>
#include <regex>
#include <iostream>
struct _String {
	const char* value;
    size_t length;
	_String(const char* val, size_t len);
    _String();
	_String operator+(_String other);
	_String operator*(uint32_t times);
	static _String fromChars(char c, size_t times);
    size_t u8length();
	_String cpAt(size_t ind);
    friend std::istream &operator>>(std::istream &in, _String &str);
    friend std::ostream &operator<<(std::ostream &os, const _String &str);

	template<typename Fn>
	void withStr(Fn fn) {

		if (value[length] == 0) {
			auto s = new char[length + 1];
			strncpy(s, value, length * sizeof(char));
			s[length] = '\0';

			const char* arg = s;
			fn(arg);
			delete[] s;
		}
		else
			fn(value);

	}
};
typedef _String String;


typedef struct _Array {
    size_t length;
	char value[0];
}*Array;

struct Span {
	char* value;
    size_t length;
};
struct StringSpan {
	String* value;
    size_t length;
};
enum FileMode {
	CreateNew, OpenOrCreate, AppendOrCreate, Open
};
enum StreamPos {
	BEGINNING, CURRENT_POSITION, END
};
struct strsearchContext {
	std::cregex_iterator it;
	std::regex r;
};
