#pragma once

#include <stdint.h>

#ifdef _WIN32
#define OVERFLOAT_API __declspec(dllimport)
#else
#define OVERFLOAT_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

OVERFLOAT_API int overfloat_version_major(void);
OVERFLOAT_API int overfloat_version_minor(void);
OVERFLOAT_API int overfloat_version_patch(void);
OVERFLOAT_API void* overfloat_spec_create(int exponent_bits, int mantissa_bits, int rounding_mode);
OVERFLOAT_API void overfloat_spec_free(void* spec_handle);
OVERFLOAT_API int overfloat_spec_exponent_bits(void* spec_handle);
OVERFLOAT_API int overfloat_spec_mantissa_bits(void* spec_handle);
OVERFLOAT_API int overfloat_spec_rounding_mode(void* spec_handle);
OVERFLOAT_API int overfloat_spec_validate(void* spec_handle);

OVERFLOAT_API void* overfloat_number_parse(void* spec_handle, const char* text_utf8);
OVERFLOAT_API void overfloat_number_free(void* number_handle);
OVERFLOAT_API void* overfloat_number_add(void* left_handle, void* right_handle);
OVERFLOAT_API void* overfloat_number_subtract(void* left_handle, void* right_handle);
OVERFLOAT_API void* overfloat_number_multiply(void* left_handle, void* right_handle);
OVERFLOAT_API void* overfloat_number_divide(void* left_handle, void* right_handle);
OVERFLOAT_API int overfloat_number_classification(void* number_handle);
OVERFLOAT_API int overfloat_number_is_negative(void* number_handle);
OVERFLOAT_API int overfloat_number_binary_exponent(void* number_handle);
OVERFLOAT_API int overfloat_number_format(void* number_handle, char* buffer_utf8, int buffer_length);

#ifdef __cplusplus
}
#endif
