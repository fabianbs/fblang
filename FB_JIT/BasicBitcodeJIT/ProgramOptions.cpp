/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#include "ProgramOptions.h"
#include <boost/program_options.hpp>
#include <string>
#include <vector>
#include <gc.h>

namespace po = boost::program_options;

struct PO_Data {
    po::options_description desc;
    po::variables_map vm;
    po::positional_options_description pdesc;
    PO_Data(std::string caption) :desc(caption), vm(), pdesc() {

    }
};

FB_INTERNAL(void) *SetupProgramOptions() noexcept(false) {
    PO_Data *ret = GC_NEW(PO_Data);
    if (!ret)
        throw std::bad_alloc();
    new(ret) PO_Data("Avaliable options");
    return ret;
}

FB_INTERNAL(void) AddOptionFlag(PO_Data *dat, bool *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<bool>(result)->implicit_value(true)->default_value(false), description);
}

FB_INTERNAL(void) AddOptionI32(PO_Data *dat, int32_t *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<int32_t>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionI64(PO_Data *dat, int64_t *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<int64_t>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionU32(PO_Data *dat, uint32_t *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<uint32_t>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionU64(PO_Data *dat, uint64_t *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<uint64_t>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionFloat(PO_Data *dat, float *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<float>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionDouble(PO_Data *dat, double *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<double>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionString(PO_Data *dat, String *result, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<String>(result)->required(), description);
}

FB_INTERNAL(void) AddOptionFlagDflt(PO_Data *dat, bool *result, bool dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<bool>(result)->implicit_value(true)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionI32Dflt(PO_Data *dat, int32_t *result, int32_t dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<int32_t>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionI64Dflt(PO_Data *dat, int64_t *result, int64_t dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<int64_t>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionU32Dflt(PO_Data *dat, uint32_t *result, uint32_t dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<uint32_t>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionU64Dflt(PO_Data *dat, uint64_t *result, uint64_t dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<uint64_t>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionFloatDflt(PO_Data *dat, float *result, float dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<float>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionDoubleDflt(PO_Data *dat, double *result, double dflt, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<double>(result)->default_value(dflt), description);
}

FB_INTERNAL(void) AddOptionStringDflt(PO_Data *dat, String *result, const char *dflt, size_t dflt_len, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<String>(result)->default_value(String(dflt, dflt_len)), description);
}

template<typename T>
boost::function1<void, const std::vector<T> &> basic_notifier(T **result, size_t *resultc) {
    return [result, resultc] (const std::vector<T> &vec) {
        *result = (T *)GC_MALLOC_ATOMIC(sizeof(T) * vec.size());
        memcpy(*result, vec.data(), sizeof(T) * vec.size());
        *resultc = vec.size();
    };
}



FB_INTERNAL(void) AddPositionalI32(PO_Data *dat, int32_t **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<int32_t>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalI64(PO_Data *dat, int64_t **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<int64_t>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalU32(PO_Data *dat, uint32_t **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<uint32_t>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalU64(PO_Data *dat, uint64_t **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<uint64_t>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalFloat(PO_Data *dat, float **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<float>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalDouble(PO_Data *dat, double **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<double>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) AddPositionalString(PO_Data *dat, String **result, size_t *resultc, int maxc, const char *name, const char *description) {
    assert(dat);
    dat->desc.add_options()(name, po::value<std::vector<String>>()->notifier(basic_notifier(result, resultc)), description);
    dat->pdesc.add(name, maxc);
}

FB_INTERNAL(void) ParseOptions(PO_Data *dat, int argc, const char *argv[]) {
    assert(dat);
    try {
        po::store(po::command_line_parser(argc, argv).options(dat->desc).positional(dat->pdesc).run(), dat->vm);
        po::notify(dat->vm);
    }
    catch (std::exception &e) {
        std::cerr << e.what() << std::endl;
        std::cerr << dat->desc << std::endl;
        exit(1);
    }
    catch (...) {
        std::cerr << "An unexpected error occurred" << std::endl;
        std::cerr << dat->desc << std::endl;
        exit(1);
    }
}
