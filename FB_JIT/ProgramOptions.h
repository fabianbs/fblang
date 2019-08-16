#pragma once
#ifndef FB_INTERNALS
#define FB_INTERNALS
#ifdef EXPORT_SYMBOLS
#define FB_INTERNAL(x)extern "C" x __declspec(dllexport)
#else 
#define FB_INTERNAL(x)extern "C" x __declspec(dllimport)
#endif
#endif // !FB_INTERNALS

#include <cstdint>
#include "Structures.h"
struct PO_Data;


FB_INTERNAL(void) *SetupProgramOptions()noexcept(false);
FB_INTERNAL(void) AddOptionFlag(PO_Data *dat, bool *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionI32(PO_Data *dat, int32_t *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionI64(PO_Data *dat, int64_t *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionU32(PO_Data *dat, uint32_t *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionU64(PO_Data *dat, uint64_t *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionFloat(PO_Data *dat, float *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionDouble(PO_Data *dat, double *result, const char *name, const char *description);
FB_INTERNAL(void) AddOptionString(PO_Data *dat, String *result, const char *name, const char *description);

FB_INTERNAL(void) AddOptionFlagDflt(PO_Data *dat, bool *result, bool dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionI32Dflt(PO_Data *dat, int32_t *result, int32_t dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionI64Dflt(PO_Data *dat, int64_t *result, int64_t dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionU32Dflt(PO_Data *dat, uint32_t *result, uint32_t dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionU64Dflt(PO_Data *dat, uint64_t *result, uint64_t dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionFloatDflt(PO_Data *dat, float *result, float dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionDoubleDflt(PO_Data *dat, double *result, double dflt, const char *name, const char *description);
FB_INTERNAL(void) AddOptionStringDflt(PO_Data *dat, String *result, const char *dflt, size_t dflt_len, const char *name, const char *description);


FB_INTERNAL(void) AddPositionalI32(PO_Data *dat, int32_t **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalI64(PO_Data *dat, int64_t **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalU32(PO_Data *dat, uint32_t **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalU64(PO_Data *dat, uint64_t **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalFloat(PO_Data *dat, float **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalDouble(PO_Data *dat, double **result, size_t *resultc, int maxc, const char *name, const char *description);
FB_INTERNAL(void) AddPositionalString(PO_Data *dat, String **result, size_t *resultc, int maxc, const char *name, const char *description);


FB_INTERNAL(void) ParseOptions(PO_Data *dat, int argc, const char *argv[]);