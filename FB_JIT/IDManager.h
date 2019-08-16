#pragma once
#include <vector>
#include <deque>
template<typename T>
class IDManager
{
	std::vector<T> values;
	std::deque<uint32_t> empty;
	inline bool isEmptyID(uint32_t id) const{
		for (auto it = empty.cbegin(); it != empty.cend(); ++it) {
			if (*it == id)
				return true;
		}
		return false;
	}
public:
	IDManager() {}
	~IDManager() {}
	uint32_t addValue(const T& val) {
		uint32_t ret;
		if (empty.empty()) {
			ret = values.size();
			values.push_back(val);
		}
		else {
			ret = empty.front();
			empty.pop_front();
			values[ret] = val;
		}
		return ret;
	}
	uint32_t addValue(T&& val) {
		uint32_t ret;
		if (empty.empty()) {
			ret = values.size();
			values.push_back(val);
		}
		else {
			ret = empty.front();
			empty.pop_front();
			values[ret] = val;
		}
		return ret;
	}
	const T& lookupValue(uint32_t id) const{
		return values.at(id);
	}
	void removeValue(uint32_t id) {
		empty.push_back(id);
	}
	bool hasValue(uint32_t id) const{
		return id < values.size() && !isEmptyID(id);
	}
};

