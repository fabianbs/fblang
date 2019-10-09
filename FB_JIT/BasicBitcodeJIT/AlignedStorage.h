/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <memory>

template<typename T, size_t alignment>
class AlignedStorageBase {
protected:
    union {
        T value;
        char padding[alignment];
    } value;
public:
    AlignedStorageBase(T&& value) {
        this->value.value = std::move(value);
    }
    AlignedStorageBase(const T& value) {
        this->value.value = value;
    }
    operator T() const {
        return value.value;
    }
   /* AlignedStorageBase<T, alignment>& operator=(T&& val) {
        value.value = std::move(val);
        return *this;
    }
    AlignedStorageBase<T, alignment>& operator=(const T& val) {
        value.value = val;
        return *this;
    }
    bool operator==(const AlignedStorage<T, alignment>& other)const {
        return value.value == other.value.value;
    }
    bool operator==(const T& other)const {
        return value.value == other;
    }
    friend bool operator==(const T& other, const AlignedStorage<T, alignment>& ali) {
        return ali.value.value == other;
    }*/
};
template<typename T, size_t alignment>
class AlignedStorage :public AlignedStorageBase<T, alignment> {
public:
    AlignedStorage<T, alignment>& operator=(T&& val)  {
        value.value = std::move(val);
        return *this;
    }
    AlignedStorage<T, alignment>& operator=(const T& val) {
        value.value = val;
        return *this;
    }
};
template<size_t alignment>
class AlignedStorage<uint32_t, alignment> :public AlignedStorageBase<uint32_t, alignment> {
public:
    AlignedStorage<uint32_t, alignment>& operator=(uint32_t val) {
        value.value = val;
        return *this;
    }
    uint32_t operator++() {
        return ++value.value;
    }
    uint32_t operator++(int) {
        return value.value++;
    }

};