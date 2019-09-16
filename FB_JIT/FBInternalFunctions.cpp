/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "FBInternalFunctions.h"

#include <iostream>
#include <string>
#include <cstdio>

#include <cstdint>
#include <cstring>
#include <sstream>
#include <unordered_set>


#include <stdarg.h>
#include <mutex>
#include <condition_variable>
#include <regex>
#include <exception>
#include <stdexcept>

#include "IDManager.h"
#include <atomic>
#include <utf8.h>
#include <llvm/Support/FormatVariadic.h>
#include <llvm/ADT/APInt.h>
#include <llvm/ADT/STLExtras.h>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/Hashing.h>
#include "formatStrings.h"
#include "GC_NEW.h"
#include <gc.h>
#define STRETURN(str){*_ret=(str);return;}

#ifdef WIN32
#define ENDL "\r\n"
#define ENDL_LEN 2
#else
#define ENDL "\n"
#define ENDL_LEN 1
#endif

std::unordered_set<std::string> stringpool;

std::mutex stringpoolMx;
std::mutex stdOutMx;
static llvm::raw_ostream &couts() {
    std::error_code ec;
    static llvm::raw_fd_ostream os(1, false, true);
    assert(!ec);
    return os;
}
//IDManager<FILE*> openFiles;
String fromStringPool(std::string &&str) {

    /*auto it = stringpool.find(str);
    if (it == stringpool.end())
    {
        stringpool.insert(str);
        it = stringpool.find(str);
    }*/
    std::lock_guard<decltype(stringpoolMx)> lock(stringpoolMx);
    String toret = { /*it->c_str()*/stringpool.insert(str).first->data(), (uint32_t)str.size() };

    return toret;
}
//copied from "https://thispointer.com/find-and-replace-all-occurrences-of-a-sub-string-in-c/"
void findAndReplaceAll(std::string &data, std::string &&toSearch, const std::string &replaceStr) {
    // Get the first occurrence
    size_t pos = data.find(toSearch);

    // Repeat till end is reached
    while (pos != std::string::npos) {
        // Replace this occurrence of Sub String
        data.replace(pos, toSearch.size(), replaceStr);
        // Get the next occurrence from the current position
        pos = data.find(toSearch, pos + toSearch.size());
    }
}
String fromStringPool(char *buf, uint32_t len) {

    std::lock_guard<decltype(stringpoolMx)> lock(stringpoolMx);
    String toret = { stringpool.emplace(buf, len).first->data(), len };
    return toret;
}


void negate(int64_t hi, int64_t lo, uint64_t &retHi, uint64_t &retLo) {
    retLo = (uint64_t)(-lo);
    retHi = (uint64_t)(lo == 0 ? -hi : ~hi);
}
uint64_t rem(uint64_t hi, uint64_t lo, uint32_t mod) {
    const uint64_t bas = (uint64_t)~0u + 1;
    uint64_t ret = 0;
    uint32_t a[4];
    ((uint64_t *)a)[0] = hi;
    ((uint64_t *)a)[1] = lo;

    for (int i = 0; i < 3; ++i) {
        ret = (ret * (bas % mod) + a[i] % mod) % mod;
    }

    return ret;
}
void divide(uint64_t hi, uint64_t lo, uint64_t n, uint64_t &retHi, uint64_t &retLo) {
    uint64_t cur = 0;
    uint32_t a[4];
    ((uint64_t *)a)[0] = hi;
    ((uint64_t *)a)[1] = lo;
    uint32_t ans[4] = { 0,0,0,0 };
    auto pret = &ans[3];
    const uint64_t bas = (uint64_t)~0u + 1;
    for (int i = 0; i < 3; ++i) {
        cur = (cur * (uint64_t)bas + (uint64_t)a[i]);
        *pret = cur / n;
        pret--;
        cur %= n;
    }
    retHi = ((uint64_t *)ans)[0];
    retLo = ((uint64_t *)ans)[1];
}
FB_INTERNAL(void) cprintln(const char *str, size_t len) {
    //static uint32_t counter = 0;
    std::lock_guard<decltype(stdOutMx)> lck(stdOutMx);
    //couts() << ">> Print string of length " << len << "\r\n";
    //if (len < 32)
    couts() << llvm::StringRef(str, len) << ENDL;
    //couts().flush();
    //std::cout << std::string(str, len) << "::" << ++counter << std::endl;
    //printf("%.*s\n", len, str);
    //fflush(stdout);
}

FB_INTERNAL(void) cprintnwln() {
    std::lock_guard<decltype(stdOutMx)> lck(stdOutMx);
    couts() << ENDL;
    //couts().flush();
}

FB_INTERNAL(void) cprint(const char *str, size_t len) {
    std::lock_guard<decltype(stdOutMx)> lck(stdOutMx);
    couts() << llvm::StringRef(str, len);
}

FB_INTERNAL(int) cprintf(const char *str, size_t len, ...) {
    std::lock_guard<decltype(stdOutMx)> lck(stdOutMx);
    va_list v;
    va_start(v, len);
    std::string fstr(str, len);
    findAndReplaceAll(fstr, "%s", "%.*s");
    auto ret = vprintf(fstr.c_str(), v);
    va_end(v);
    return ret;
}


FB_INTERNAL(int) cscanf(const char *str, size_t len, ...) {
    va_list v;
    va_start(v, len);
    std::string format(str, len);
    auto ret = vscanf_s(format.c_str(), v);
    va_end(v);
    return ret;
}

FB_INTERNAL(void) creadln(String *ret) {
    std::string line;
    std::getline(std::cin, line);
    *ret = fromStringPool(std::move(line));
}

FB_INTERNAL(int32_t) creadky() {
    return getchar();
}


FB_INTERNAL(void)  to_str(int i, String *ret) {
    /*if (i == 0) {
        printf("zero\n");
    }*/
    auto str = std::to_string(i);

    *ret = fromStringPool(std::move(str));
}

FB_INTERNAL(void) uto_str(uint32_t ui, String *ret) {
    auto str = std::to_string(ui);

    *ret = fromStringPool(std::move(str));
}

FB_INTERNAL(void) zto_str(size_t z, String *ret) {
    auto str = std::to_string(z);

    *ret = fromStringPool(std::move(str));
}

FB_INTERNAL(void) lto_str(int64_t i, String *ret) {
    auto str = std::to_string(i);

    *ret = fromStringPool(std::move(str));
}

FB_INTERNAL(void) ulto_str(uint64_t ui, String *ret) {
    auto str = std::to_string(ui);

    *ret = fromStringPool(std::move(str));
}

FB_INTERNAL(void) llto_str(int64_t _hi, int64_t _lo, String *ret) {
    /*bool negative = _hi < 0;
    uint64_t hi, lo;
    if (negative) {
        negate(_hi, _lo, hi, lo);
    }
    else {
        hi = (uint64_t)_hi;
        *((int64_t*)&lo) = _lo;
    }

    const uint64_t lowBound = 1e9;
    std::stringstream s;
    while (hi != 0) {
        s << rem(hi, lo, lowBound);
        divide(hi, lo, lowBound, hi, lo);
    }
    auto r = s.str();
    std::string neg = negative ? "-" : "";
    if (r.size() > 0 && lo == 0) {
        *ret = fromStringPool(neg + std::string(r.rbegin(), r.rend()));
    }
    else {
        *ret = fromStringPool(neg + std::to_string(lo) + std::string(r.rbegin(), r.rend()));
    }*/
    uint64_t hi, lo;
    hi = *((uint64_t *)& _hi);
    lo = *((uint64_t *)& _lo);
    llvm::APInt val(128u, { hi, lo });
    std::string str;
    llvm::raw_string_ostream os(str);
    os << val;
    *ret = fromStringPool(std::move(os.str()));
}

FB_INTERNAL(void) dto_str(double d, String *ret) {
    *ret = fromStringPool(std::to_string(d));
}

FB_INTERNAL(void) fto_str(float f, String *ret) {
    *ret = fromStringPool(std::to_string(f));
}

FB_INTERNAL(void) span_to_str(Span s, String *ret) {
    *ret = fromStringPool(std::string(s.value, s.length));
}

FB_INTERNAL(int32_t) stoi(String str) {
    return std::stoi(std::string(str.value, str.length));
}

FB_INTERNAL(int64_t) stol(String str) {
    return std::stoll(std::string(str.value, str.length));
}

FB_INTERNAL(uint64_t) stoul(String str) {
    return std::stoull(std::string(str.value, str.length));
}

FB_INTERNAL(double) stof(String str) {
    return std::stod(std::string(str.value, str.length));
}

FB_INTERNAL(void) strconcat(const char *str1, size_t len1, const char *str2, size_t len2, String *ret) {

    *ret = fromStringPool(std::string(str1, len1) + std::string(str2, len2));
}

FB_INTERNAL(void) strmulticoncat(String *strs, size_t strc, String *_ret) {
    if (strc == 0)
        STRETURN(fromStringPool(""));

    uint32_t len = 0;
    for (uint32_t i = 0; i < strc; ++i) {
        uint32_t nwLen = len + strs[i].length;
        if (nwLen < len)
            STRETURN(fromStringPool(""));// overflow
        len = nwLen;
    }
    std::string ret;
    ret.reserve(len);

    for (uint32_t i = 0; i < strc; ++i) {
        ret.append(strs[i].value, strs[i].length);
    }
    STRETURN(fromStringPool(std::move(ret)));
}

FB_INTERNAL(void) strmul(const char *str, size_t str_len, uint32_t factor, String *_ret) {
    if (factor == 0u) {
        STRETURN(fromStringPool(""))
    }
    uint32_t len = factor * str_len;
    //overflow
    if (len / factor != str_len) {
        STRETURN(fromStringPool(""))
    }
    std::string ret;
    ret.reserve(len);

    for (uint32_t i = 0; i < factor; ++i) {
        ret.append(str, str_len);
    }

    STRETURN(fromStringPool(std::move(ret)))
}

FB_INTERNAL(void) stralloc(char c, size_t count, String *_ret) {
    STRETURN(fromStringPool(std::string(count, c)))
}

FB_INTERNAL(int32_t) strcompare(const char *lhs, size_t lhs_len, const char *rhs, size_t rhs_len) {
    return strncmp(lhs, rhs, lhs_len >= rhs_len ? lhs_len : rhs_len);
}

FB_INTERNAL(bool) strequals(const char *lhs, size_t lhs_len, const char *rhs, size_t rhs_len) {
    if (lhs_len != rhs_len)return false;
    if (lhs == rhs)return true;
    return memcmp(lhs, rhs, lhs_len) == 0;
}

FB_INTERNAL(bool) isMatch(char *str_value, size_t str_length, char *pattern_value, size_t pattern_length) {
    return std::regex_match(str_value, str_value + str_length, std::regex(pattern_value, pattern_value + pattern_length));
}

FB_INTERNAL(void) strsplit_init(const char *str, size_t str_len, String *save) {
    if (str_len == 0) {
        save->value = nullptr;
        save->length = 0;
    }
    else {
        save->value = str;
        save->length = str_len;
    }
}

FB_INTERNAL(bool) strsplit(const char *delims, size_t delims_len, String *outp, String *save) {

    if (save == nullptr || outp == nullptr)
        return false;
    String s = *save;
    if (s.length == 0)
        return false;


    uint32_t len;
    for (len = 0u; len < s.length; ++len) {
        for (uint32_t i = 0; i < delims_len; ++i) {
            if (s.value[len] == delims[i]) {
                goto loop_end;
            }
        }
    }
loop_end:
    outp->value = s.value;
    outp->length = len;
    //skip outp and the delim
    save->value += len;
    save->value++;

    save->length -= len - 1;
    return true;
}

FB_INTERNAL(void) *strsearch_init(const char *str, size_t str_len, const char *pattern, size_t pattern_len) {
    if (str_len == 0)
        return nullptr;
    auto ret = gc_newT<strsearchContext>();
    ret->r = std::regex(pattern, pattern + pattern_len);
    ret->it = std::cregex_iterator(str, str + str_len, ret->r);
    return ret;
}

FB_INTERNAL(bool) strsearch(void *ctx, String *match) {
    if (ctx == nullptr)
        return false;
    auto context = (strsearchContext *)ctx;
    if (context->it == std::cregex_iterator())
        return false;

    *match = fromStringPool(context->it->str());
    context->it++;
    return true;
}

FB_INTERNAL(void) *strscan_init_pattern(String *patterns, size_t patternc) {
    auto ret = gc_newT<std::regex>();
    std::string str;
    llvm::raw_string_ostream s(str);
    bool has = false;
    for (const String &pattern : llvm::ArrayRef<String>(patterns, patternc)) {
        if (has)
            s << '|';
        else
            has = true;

        s << '(' << llvm::StringRef(pattern.value, pattern.length) << ')';

    }
    s.flush();
    *ret = std::regex(str);
    return ret;
}

FB_INTERNAL(void) *strscan_init(const char *inp, size_t inp_len, void *pRegex) {
    if (inp_len == 0 || pRegex == nullptr)
        return nullptr;
    auto ret = gc_newT<std::cregex_iterator>();
    *ret = std::cregex_iterator(inp, inp + inp_len, *(std::regex *)pRegex);
    return ret;
}

FB_INTERNAL(bool) strscan(void *ctx, String *matchStr, uint32_t *groupID, uint32_t *pos) {
    if (ctx == nullptr || matchStr == nullptr || groupID == nullptr || pos == nullptr)
        return false;
    auto it = (std::cregex_iterator *)ctx;
    if (*it == std::cregex_iterator())
        return false;
    auto match = **it;
    ++(*it);
    *groupID = 0;
    *matchStr = fromStringPool(match.str());
    *pos = match.position();
    for (auto mIt = match.begin() + 1; mIt != match.end(); ++mIt, ++(*groupID)) {
        if (mIt->length() != 0) {
            break;
        }
    }
    return true;
}

FB_INTERNAL(int) string_scanf(char *strVal, size_t strLen, char *format, size_t formatLen, ...) {
    va_list v;
    va_start(v, formatLen);
    std::string fstr(format, formatLen);
    std::string vstr(strVal, strLen);
    auto ret = vsscanf_s(vstr.c_str(), fstr.c_str(), v);
    va_end(v);
    return ret;
}

FB_INTERNAL(void) str_format(char *strVal, size_t strLen, String *ret, ...) {
    va_list v;
    va_start(v, ret);

    String(strVal, strLen).withStr([=] (const char *fmt) {
        char *retVal;
        uint32_t retLen = vasprintf(&retVal, strVal, v);
        *ret = { retVal, retLen };
    });

    va_end(v);

}

FB_INTERNAL(size_t) str_hash(char *strVal, size_t strLen) {
    return llvm::hash_value(llvm::StringRef(strVal, strLen));
}


FB_INTERNAL(size_t) u8string_len(char *strVal, size_t strLen) {
    return String(strVal, strLen).u8length();
}

FB_INTERNAL(void) *u8string_it(char *strVal, size_t strLen) {
    auto ret = GC_NEW(utf8::unchecked::iterator<const char *>);
    ret->iterator<const char *>::iterator(strVal);
    return ret;
}

FB_INTERNAL(bool) u8str_it_tryGetNext(void *_it, uint32_t *cp) {
    auto it = (utf8::unchecked::iterator<const char *> *)_it;
    auto end = utf8::unchecked::iterator<const char *>();
    if (*it != end) {
        *cp = **it;
        ++ *it;

        return true;
    }
    return false;
}

FB_INTERNAL(void) u8str_codepoint_at(char *strVal, size_t strLen, size_t index, String *_ret) {
    String s(strVal, strLen);
    *_ret = s.cpAt(index);
    _ret->length = max(0u, min(_ret->length, strLen - index));
}

FB_INTERNAL(void) gc_init() {
    GC_INIT();
    GC_enable_incremental();
    GC_allow_register_threads();
}

FB_INTERNAL(void) *gc_new(size_t size)noexcept(false) {
    auto ret = GC_MALLOC(size);
    if (ret == nullptr)
        throw std::bad_alloc();
    //memset(ret, 0, size);
    return ret;
}


FB_INTERNAL(void) *gc_new_with_dtor(size_t size, void(*finalizer)(void *, void *))noexcept(false) {
    auto ret = gc_new(size);
    GC_REGISTER_FINALIZER(ret, finalizer, 0, 0, 0);
    return ret;
}

FB_INTERNAL(void) *gc_resize_array(void *array, size_t newSize) noexcept(false) {
    auto ret = GC_REALLOC(array, newSize);
    if (ret == nullptr)
        throw std::bad_alloc();
    return ret;
}

FB_INTERNAL(Span) to_span(Array arr) {
    if (arr == nullptr) {
        Span defaultSpan = { nullptr, 0u };
        return defaultSpan;
    }
    else {
        Span arrSpan = { &arr->value[0], arr->length };
        return arrSpan;
    }
}

FB_INTERNAL(void) *openFile(String filename, FileMode mode) {
    const char *pMode;
    switch (mode) {
        case FileMode::CreateNew:pMode = "w";
            break;
        case FileMode::AppendOrCreate:pMode = "a+";
            break;
        case FileMode::Open:pMode = "r";
            break;
        case FileMode::OpenOrCreate:pMode = "r+";
            break;
        default:
            pMode = "";
            break;
    }
    return fopen(filename.value, pMode);
}

FB_INTERNAL(void) closeFile(void *fileHandle) {
    fclose((FILE *)fileHandle);
}

FB_INTERNAL(uint32_t) writeFile(void *hFile, Span bytes) {
    return fwrite(bytes.value, sizeof(char), bytes.length, (FILE *)hFile);
}

FB_INTERNAL(uint32_t) readFile(void *hFile, Span buf) {
    return fread(buf.value, sizeof(char), buf.length, (FILE *)hFile);
}

FB_INTERNAL(uint64_t) getFileStreamPos(void *hFile) {
    return _ftelli64((FILE *)hFile);
}

FB_INTERNAL(bool) setfileStreamPos(void *hFile, int64_t pos, StreamPos relativeTo) {
    int origin = 0;
    switch (relativeTo) {
        case StreamPos::BEGINNING:origin = SEEK_SET; break;
        case StreamPos::CURRENT_POSITION:origin = SEEK_CUR; break;
        case StreamPos::END:origin = SEEK_END; break;
    }
    return _fseeki64((FILE *)hFile, pos, origin) != 0;
}

FB_INTERNAL(bool) isPrime(uint64_t numb) {
    static uint64_t lowerPrimes[] = { 2,3,5,7,11,13,17,19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71 };
    if (!(numb & 1))
        return numb == 2;

    uint64_t end = (uint64_t)std::sqrt(numb) + 1;

    for (int i = 0; i < 20 && lowerPrimes[i] < end; ++i) {
        if (numb % lowerPrimes[i] == 0)
            return false;
    }

    for (uint16_t i = 73; i < end; i += 2) {
        if (numb % i == 0)
            return false;
    }
    return true;
}

inline bool isPrimeSZ(size_t numb) {
    static const size_t lowerPrimes[] = { 2,3,5,7,11,13,17,19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71 };
    
    if (!(numb & 1))
        return numb == 2;

    size_t end = (size_t)std::sqrt(numb) + 1;

    for (int i = 0; i < 20 && lowerPrimes[i] < end; ++i) {
        if (numb % lowerPrimes[i] == 0) {
            //printf("isPrimeSZ(%zu) = false", numb);
            return false;
        }
            
    }

    for (size_t i = 73; i < end; i += 2) {
        if (numb % i == 0) {
            //printf("isPrimeSZ(%zu) = false", numb);
            return false;
        }
    }
    //printf("isPrimeSZ(%zu) = true", numb);
    return true;
}

FB_INTERNAL(size_t) nextPrime(size_t numb) {
    if (numb < 5)// We dont want too small primes ...
        return 5;
    numb |= 1;
    while (!isPrimeSZ(numb)) {
        numb += 2;
    }
    return numb;
}

FB_INTERNAL(void) *gc_new_mutex() {
    auto ret = (std::recursive_mutex *) GC_MALLOC(sizeof(std::recursive_mutex));
    ret->recursive_mutex::recursive_mutex();
    return ret;
}

FB_INTERNAL(void) *new_lock(void *mx) {
    return new std::unique_lock<std::recursive_mutex>(*(std::recursive_mutex *)mx);
}

FB_INTERNAL(void) *gc_new_cv() {
    auto ret = (std::condition_variable_any *)GC_MALLOC(sizeof(std::condition_variable_any));
    ret->condition_variable_any::condition_variable_any();
    return ret;
}

FB_INTERNAL(void) unlock(void *lck) {
    auto lock = (std::unique_lock<std::recursive_mutex> *)lck;
    delete lock;
}

FB_INTERNAL(void) waitFor(void *cv, void *lck) {
    ((std::condition_variable_any *)cv)->wait(*(std::unique_lock<std::recursive_mutex> *)lck);
}

FB_INTERNAL(bool) waitForMillis(void *cv, void *lck, uint64_t millis) {
    auto cond = (std::condition_variable_any *)cv;
    auto lock = (std::unique_lock<std::recursive_mutex> *)lck;
    std::chrono::milliseconds ms(millis);
    return cond->wait_for(*lock, ms) == std::cv_status::no_timeout;
}

FB_INTERNAL(void) notify_one(void *cv) {
    ((std::condition_variable_any *)cv)->notify_one();
}
FB_INTERNAL(void) notify_all(void *cv) {
    ((std::condition_variable_any *)cv)->notify_all();
}

FB_INTERNAL(void) *mpsc_enqueue(void **_head, void *item) {
    auto head = (std::atomic<void *> *)_head;
    auto oldValue = head->load(std::memory_order_acquire);
    do {

    }
    while (!head->compare_exchange_weak(oldValue, item, std::memory_order_release, std::memory_order_acquire));
    return oldValue;
}

FB_INTERNAL(void) throwException(const char *msg, uint32_t msgLen)noexcept(false) {
    throw std::runtime_error(std::string(msg, msgLen));
}

FB_INTERNAL(void) throwOutOfBounds(uint32_t index, uint32_t length)noexcept(false) {
    std::stringstream s;
    s << "The index " << index << " is outside of the expected range [0, " << length << ").";
    throw std::out_of_range(s.str());
}

FB_INTERNAL(void) throwOutOfBounds64(uint64_t index, uint64_t length) noexcept(false) {
    std::stringstream s;
    s << "The index " << index << " is outside of the expected range [0, " << length << ").";
    throw std::out_of_range(s.str());
}

FB_INTERNAL(void) throwNullDereference(const char *msg, uint32_t msgLen)noexcept(false) {
    if (msgLen > 0)
        throw std::runtime_error("Null-Dereference: " + std::string(msg, msgLen));
    throw std::runtime_error("Null-Dereference");
}

FB_INTERNAL(void) throwIfNull(void *ptr, const char *msg, uint32_t msgLen) noexcept(false) {
    if (ptr == nullptr) {
        if (msgLen > 0)
            throw std::runtime_error("Null-Dereference: " + std::string(msg, msgLen));
        throw std::runtime_error("Null-Dereference");
    }
}

FB_INTERNAL(void) throwIfOutOfBounds(uint32_t index, uint32_t len) noexcept(false) {
    if (index >= len)throwOutOfBounds(index, len);
}

FB_INTERNAL(void) throwIfOutOfBounds64(uint64_t index, uint64_t len) noexcept(false) {
    if (index >= len)throwOutOfBounds64(index, len);
}

FB_INTERNAL(void) throwIfNullOrOutOfBounds(uint32_t index, uint32_t *len) noexcept(false) {
    if (len == nullptr)
        throwNullDereference(nullptr, 0);
    else {
        auto length = *len;
        if (index >= length)
            throwOutOfBounds(index, length);
    }
}

FB_INTERNAL(void) throwIfNullOrOutOfBounds64(uint64_t index, uint64_t *len) noexcept(false) {
    if (len == nullptr)
        throwNullDereference(nullptr, 0);
    else {
        auto length = *len;
        if (index >= length)
            throwOutOfBounds64(index, length);
    }
}


// code copied from "https://cristianadam.eu/20160914/nullpointerexception-in-c-plus-plus/", 18.02.2019, 12:00Uhr; little changes made
#include <Windows.h>
#include <eh.h>
const char *seDescription(const unsigned int &code) {
    switch (code) {
        case EXCEPTION_ACCESS_VIOLATION:         return "EXCEPTION_ACCESS_VIOLATION";
        case EXCEPTION_ARRAY_BOUNDS_EXCEEDED:    return "EXCEPTION_ARRAY_BOUNDS_EXCEEDED";
        case EXCEPTION_BREAKPOINT:               return "EXCEPTION_BREAKPOINT";
        case EXCEPTION_DATATYPE_MISALIGNMENT:    return "EXCEPTION_DATATYPE_MISALIGNMENT";
        case EXCEPTION_FLT_DENORMAL_OPERAND:     return "EXCEPTION_FLT_DENORMAL_OPERAND";
        case EXCEPTION_FLT_DIVIDE_BY_ZERO:       return "EXCEPTION_FLT_DIVIDE_BY_ZERO";
        case EXCEPTION_FLT_INEXACT_RESULT:       return "EXCEPTION_FLT_INEXACT_RESULT";
        case EXCEPTION_FLT_INVALID_OPERATION:    return "EXCEPTION_FLT_INVALID_OPERATION";
        case EXCEPTION_FLT_OVERFLOW:             return "EXCEPTION_FLT_OVERFLOW";
        case EXCEPTION_FLT_STACK_CHECK:          return "EXCEPTION_FLT_STACK_CHECK";
        case EXCEPTION_FLT_UNDERFLOW:            return "EXCEPTION_FLT_UNDERFLOW";
        case EXCEPTION_ILLEGAL_INSTRUCTION:      return "EXCEPTION_ILLEGAL_INSTRUCTION";
        case EXCEPTION_IN_PAGE_ERROR:            return "EXCEPTION_IN_PAGE_ERROR";
        case EXCEPTION_INT_DIVIDE_BY_ZERO:       return "EXCEPTION_INT_DIVIDE_BY_ZERO";
        case EXCEPTION_INT_OVERFLOW:             return "EXCEPTION_INT_OVERFLOW";
        case EXCEPTION_INVALID_DISPOSITION:      return "EXCEPTION_INVALID_DISPOSITION";
        case EXCEPTION_NONCONTINUABLE_EXCEPTION: return "EXCEPTION_NONCONTINUABLE_EXCEPTION";
        case EXCEPTION_PRIV_INSTRUCTION:         return "EXCEPTION_PRIV_INSTRUCTION";
        case EXCEPTION_SINGLE_STEP:              return "EXCEPTION_SINGLE_STEP";
        case EXCEPTION_STACK_OVERFLOW:           return "EXCEPTION_STACK_OVERFLOW";
        default:                                 return "UNKNOWN EXCEPTION";
    }
}
void seTranslator(unsigned int code, struct _EXCEPTION_POINTERS *ep) {
    if (code == EXCEPTION_ACCESS_VIOLATION || code == EXCEPTION_IN_PAGE_ERROR) {
        if (ep->ExceptionRecord->ExceptionInformation[1] == 0) {
            throwNullDereference(nullptr, 0);
        }
    }
    else if (/*code == EXCEPTION_FLT_DIVIDE_BY_ZERO ||*/
             code == EXCEPTION_INT_DIVIDE_BY_ZERO) {
        throw std::runtime_error("Division by zero");
    }

    std::ostringstream os;
    os << "Structured exception caught: " << seDescription(code);

    throw std::runtime_error(os.str().c_str());
}
void terminateHandler() {
    if (std::current_exception()) {
        try {
            throw;
        }
        catch (const std::exception &ex) {
            std::cout << "terminateHandler: " << ex.what() << std::endl;
        }
        catch (...) {
            std::cout << "terminateHandler: Unknown exception!" << std::endl;
        }
    }
    else {
        std::cout << "terminateHandler: called without an exception." << std::endl;
    }
    std::abort();
}

FB_INTERNAL(void) initExceptionHandling() {
    //std::cout << "Init exception-handling" << std::endl;
    //std::set_terminate([]() {std::cout << "Hello from unhandled exception" << std::endl; std::abort(); });
    std::set_terminate(terminateHandler);
    _set_se_translator(seTranslator);

}
