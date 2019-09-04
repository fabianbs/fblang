template<typename T>
struct iterator{
    virtual bool tryGetNext(T&) = 0;
};
template<typename T>
struct iterable{
    virtual iterator<T>& getIterator() = 0
};
template<typename T, typename U>
struct Func{
    virtual U operator()(T) = 0;
};

template<typename T1, typename T2>
struct Proc{
    virtual void operator()(T1, T2) = 0;
}

template<typename T, typename U>
class HashMap{
    typedef struct _bucket{
        U key;
        T value;
        size_t hashcode;
        char state; // 0=free, 1=full, 2=deleted
    } bucket_t;
    static bool isPrime(size_t n){
        if (n < 2)
            return false;
        if(!(n & 1))
            return n == 2;
        for(size_t i = 3; i * i <= n; i += 2){
            if(n % i == 0)
                return false;
        }
        return true;
    }
    static size_t nextPrime(size_t n){
        if(n < 5)
            return 5;
        // primes can only be odd (since we excluded '2')
        n = n | 1;
        while(n < (size_t)~0 && !isPrime(n)){
            n += 2;
        }
        return n;
    }
    void rehash(){
        size_t oldCap = cap;
        bucket_t* oldArr = arrVal;
        cap = nextPrime(cap << 1);
        arrVal = gcnew bucket_t[nwCap];
        size = 0;
        deleted = 0;
        
        for(size_t i = 0; i < oldCap; ++i){
            if(arrVal[i].state == 1){
                insert(arrVal[i].key, arrVal[i].value, arrVal[i].hashcode);
            }
        }
    }
    bool insert(U& ky, T& val, size_t hash, bool replace){
        
        bucket_t* buck = search(ky, hash);// guaranteed to be not null
        if(buck->state == 1 ){
            if(!replace)
                return false;
        }
        else
            size++;
        buck->key = ky;
        buck->value = val;
        buck->hashcode = hash;
        buck->state = 1;
        return true;
    }
    bucket_t * search(U& ky, size_t hash){
        size_t hc = hash % cap;
        const size_t _cap = cap;
        for(size_t i = hc; i < _cap; ++i){
            if(arrVal[i].state == 0){
                 return &arrVal[i];
            }
            else if(arrVal[i].state == 1){ 
                if(hash == arrVal[i].hashcode && ky.equals(arrVal[i].key)){
                    return &arrVal[i];
                }
            }
        }
        for(size_t i = 0; i < hc; ++i){
            if(arrVal[i].state == 0){
                 return &arrVal[i];
            }
            else if(arrVal[i].state == 1){ 
                if(hash == arrVal[i].hashcode && ky.equals(arrVal[i].key)){
                    return &arrVal[i];
                }
            }
        }
        // will probably not happen
        return nullptr;
    }
    size_t load(){
        return size + deleted;
    }
    struct KeyIterator: public iterator<U>{
        bucket_t * arrVal;
        size_t cap;
        size_t i = 0;
        KeyIterator(bucket_t* arrVal, size_t cap)
            :arrVal(arrVal), cap(cap){}
        virtual bool tryGetNext(U & nxt){
            for(;i < cap; ++i){
                if(arrVal[i].state == 1) {
                    nxt = arrVal[i++].key;
                    return true;
                }
            }
            return false;
        }
    };
    struct ValueIterator: public iterator<T>{
        bucket_t * arrVal;
        size_t cap;
        size_t i = 0;
        ValueIterator(bucket_t* arrVal, size_t cap)
            :arrVal(arrVal), cap(cap){}
        virtual bool tryGetNext(T & nxt){
            for(;i < cap; ++i){
                if(arrVal[i].state == 1) {
                    nxt = arrVal[i++].value;
                    return true;
                }
            }
            return false;
        }
    };
public:
    const float LOAD_FACTOR = 0.8f;
    
    T& operator[](U ky){
        auto buck = search(ky, ky.getHashCode());
        if(buck && buck->state == 1)
            return buck->value;
        else
            throw std::exception("Key not found");
    }
    void _operator_indexer_set(U ky, T val){
        insert(ky, val, true);
    }
    bool tryGetValue(U ky, T& retVal){
        auto buck = search(ky, ky.getHashCode());
        if(buck && buck->state == 1){
            retVal = buck.value;
            return true;
        }
        return false;
    }
    T getOrElse(U ky, T els){
        T ret;
        if(tryGetValue(ky, ret))
            return ret;
        return els;
    }
    bool contains(U ky){
        T unused;
        return tryGetValue(ky, unused);
    }
    size_t count(){
        return size;
    }
    
    
    bool insert(U ky, T val, bool replace){
        if(load() > LOAD_FACTOR * cap)
            rehash();
        return insert(ky, val, ky.getHashCode(), replace);
    }
    size_t insertZip(iterable<U>& kys, iterable<T>& vals, bool replace){
        iterator<U> kyIt = kys.getIterator();
        iterator<T> valIt = vals.getIterator();
        
        U ky;
        T val;
        size_t ret = 0;
        
        while(kyIt.tryGetNext(ky) && valIt.tryGetNext(val)){
            if(insert(ky, val, replace))
                ret++;
        }
        return ret;
    }
    bool remove(U ky){
        auto buck = search(ky, ky.getHashCode());
        if(buck && buck->state == 1){
            buck->state = 2;
            return true;
        }
        return false;
    }
    size_t removeAll(Func<U, bool>& predicate){
        size_t ret = 0;
        for(size_t i = 0; i < cap; ++i){
            if(arrVal[i].state == 1 && predicate(arrVal[i].key)){
                arrVal[i].state = false;
                ret++;
            }
        }
        return ret;
    }
    
    iterable<U>& keys(){
        return *gcnew KeyIterator(arrVal, cap);
    }
    iterable<T>& values(){
        return *gcnew ValueIterator(arrVal, cap);
    }
    HashMap() : HashMap(5){
    }
    HashMap(size_t initCapacity)
        :size(0), deleted(0){
        cap = nextPrime(initCapacity);
        arrVal = gcnew bucket_t[cap];// gcnew initializes with 0        
    }
    
    void clear(){
        if(size > 0){
            size = 0; 
            deleted = 0;
            arrVal = gcnew bucket_t[cap = 5];
        }
    }
    always_inline void macro_forEach(Proc<U, T>& block){
        for(size_t i = 0; i < cap; ++i){
            if(arrVal[i].state == 1)
                block(arrVal[i].key, arrVal[i].value);
        }
    }
private:
    
    size_t cap, size, deleted;
    bucket_t * arrVal;
};

// TODO: maybe make iteration more efficient and the iteration order deterministic
// equal to the insertion-order