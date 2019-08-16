#include "Structures.h"
#include <gc.h>
#include <utf8.h>
#include <string>
#include <stdexcept>
#include <string_view>
namespace std {
    template<>
    struct equal_to<String> {
        bool operator()(const String &lhs, const String &rhs) {
            return lhs.length == rhs.length
                && (lhs.value == rhs.value || memcmp(lhs.value, rhs.value, lhs.length * sizeof(char)) == 0);
        }
    };
    template<>
    struct hash<String> {
        size_t operator()(const String &str) {
            auto end = str.value + str.length;
            size_t ret = (1 << 16) + 1;
            for (auto ptr = str.value; ptr != end; ++ptr) {
                ret += *ptr * 97u + 131u;
            }
            return ret;
        }
    };
}

_String::_String(const char *val, size_t len)
    :value(val), length(len) {
}
_String::_String() : value(nullptr), length(0) { }

_String _String::operator+(_String other) {
    char *retval = (char *)GC_MALLOC(length + other.length + 1);
    memcpy(retval, value, length);
    strncpy(retval + length, other.value, other.length);
    _String ret = { retval, length + other.length };
    return ret;
}
_String _String::operator*(uint32_t times) {
    if (times == 0u)
        return { NULL, 0u };
    if (times == 1u)
        return *this;
    char *retval = (char *)GC_MALLOC(times * length * sizeof(char) + 1);
    char *ptr = retval;
    for (uint32_t i = 0u; i < times; ++i, ptr += length) {
        memcpy(ptr, value, length);
    }
    *ptr = '\0';
    return { retval, length * times };
}

_String _String::fromChars(char c, size_t times) {
    auto retval = (char *)GC_MALLOC_ATOMIC(times * sizeof(char) + 1);
    memset(retval, c, times);
    retval[times * sizeof(char)] = '\0';
    return { retval, times };
}

size_t _String::u8length() {
    return utf8::unchecked::distance(value, value + length);
}

_String _String::cpAt(size_t ind) {
    char firstChar = value[ind];
    uint32_t sz;
    if (0xf0 == (0xf8 & firstChar))
        sz = 4;
    else if (0xe0 == (0xf0 & firstChar))
        sz = 3;
    else if (0xc0 == (0xe0 & firstChar))
        sz = 2;
    else
        sz = 1;
    return _String(&value[ind], sz);
}
std::istream &operator>>(std::istream &in, _String &str) {
    std::string value;
    in >> value;
    char *ptr = (char *)GC_MALLOC_ATOMIC(value.size() + 1);
    if (!ptr)
        throw std::bad_alloc();
    memcpy(ptr, value.c_str(), value.size());
    ptr[value.size()] = '\0';
    str = _String(ptr, value.size());
    return in;
}

std::ostream &operator<<(std::ostream &os, const _String &str) {
    std::string_view sw(str.value, str.length);
    return os << sw;
}
