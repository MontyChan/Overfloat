from __future__ import annotations

import math
from ctypes import CDLL, POINTER, c_char, c_char_p, c_int, c_void_p, create_string_buffer
from pathlib import Path


NumberLike = "OverfloatNumber | str | int | float"


class OverfloatError(RuntimeError):
    pass


class OverfloatNumber:
    def __init__(self, library: "OverfloatLibrary", handle: int, specification: "OverfloatSpec") -> None:
        self._library = library
        self._handle = handle
        self._specification = specification

    @property
    def specification(self) -> "OverfloatSpec":
        return self._specification

    @property
    def classification(self) -> int:
        return int(self._library._lib.overfloat_number_classification(self._handle))

    @property
    def is_negative(self) -> bool:
        return bool(self._library._lib.overfloat_number_is_negative(self._handle))

    @property
    def binary_exponent(self) -> int:
        return int(self._library._lib.overfloat_number_binary_exponent(self._handle))

    def _coerce_other(self, other: NumberLike) -> "OverfloatNumber":
        return self._specification.number(other)

    def add(self, other: NumberLike) -> "OverfloatNumber":
        coerced = self._coerce_other(other)
        return self._library._wrap_number(
            self._library._lib.overfloat_number_add(self._handle, coerced._handle),
            self._specification,
        )

    def subtract(self, other: NumberLike) -> "OverfloatNumber":
        coerced = self._coerce_other(other)
        return self._library._wrap_number(
            self._library._lib.overfloat_number_subtract(self._handle, coerced._handle),
            self._specification,
        )

    def multiply(self, other: NumberLike) -> "OverfloatNumber":
        coerced = self._coerce_other(other)
        return self._library._wrap_number(
            self._library._lib.overfloat_number_multiply(self._handle, coerced._handle),
            self._specification,
        )

    def divide(self, other: NumberLike) -> "OverfloatNumber":
        coerced = self._coerce_other(other)
        return self._library._wrap_number(
            self._library._lib.overfloat_number_divide(self._handle, coerced._handle),
            self._specification,
        )

    def close(self) -> None:
        if self._handle:
            self._library._lib.overfloat_number_free(self._handle)
            self._handle = 0

    def __str__(self) -> str:
        required = int(self._library._lib.overfloat_number_format(self._handle, None, 0))
        if required <= 0:
            raise OverfloatError("Unable to format number.")
        buffer = create_string_buffer(required)
        written = int(self._library._lib.overfloat_number_format(self._handle, buffer, len(buffer)))
        if written <= 0:
            raise OverfloatError("Unable to format number.")
        return buffer.value.decode("utf-8")

    def __repr__(self) -> str:
        return f"OverfloatNumber({str(self)!r}, exp_bits={self.specification.exponent_bits}, mantissa_bits={self.specification.mantissa_bits})"

    def __add__(self, other: NumberLike) -> "OverfloatNumber":
        return self.add(other)

    def __radd__(self, other: NumberLike) -> "OverfloatNumber":
        return self._coerce_other(other).add(self)

    def __sub__(self, other: NumberLike) -> "OverfloatNumber":
        return self.subtract(other)

    def __rsub__(self, other: NumberLike) -> "OverfloatNumber":
        return self._coerce_other(other).subtract(self)

    def __mul__(self, other: NumberLike) -> "OverfloatNumber":
        return self.multiply(other)

    def __rmul__(self, other: NumberLike) -> "OverfloatNumber":
        return self._coerce_other(other).multiply(self)

    def __truediv__(self, other: NumberLike) -> "OverfloatNumber":
        return self.divide(other)

    def __rtruediv__(self, other: NumberLike) -> "OverfloatNumber":
        return self._coerce_other(other).divide(self)

    def __neg__(self) -> "OverfloatNumber":
        return self.specification.number("-1") * self

    def __pos__(self) -> "OverfloatNumber":
        return self.specification.number(self)

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, (OverfloatNumber, str, int, float)):
            return NotImplemented
        return str(self) == str(self._coerce_other(other))

    def __del__(self) -> None:
        self.close()


class OverfloatSpec:
    def __init__(self, library: "OverfloatLibrary", handle: int) -> None:
        self._library = library
        self._handle = handle

    @property
    def exponent_bits(self) -> int:
        return int(self._library._lib.overfloat_spec_exponent_bits(self._handle))

    @property
    def mantissa_bits(self) -> int:
        return int(self._library._lib.overfloat_spec_mantissa_bits(self._handle))

    @property
    def rounding_mode(self) -> int:
        return int(self._library._lib.overfloat_spec_rounding_mode(self._handle))

    def parse(self, text: str) -> OverfloatNumber:
        handle = self._library._lib.overfloat_number_parse(self._handle, text.encode("utf-8"))
        return self._library._wrap_number(handle, self)

    def number(self, value: NumberLike) -> OverfloatNumber:
        if isinstance(value, OverfloatNumber):
            if value.specification._handle != self._handle:
                raise OverfloatError("Cannot mix numbers from different specifications.")
            return value
        if isinstance(value, str):
            return self.parse(value)
        if isinstance(value, bool):
            return self.parse(str(int(value)))
        if isinstance(value, (int, float)):
            return self.parse(repr(value))
        raise TypeError(f"Unsupported value type: {type(value).__name__}")

    def __call__(self, value: NumberLike) -> OverfloatNumber:
        return self.number(value)

    def __repr__(self) -> str:
        return (
            f"OverfloatSpec(exp_bits={self.exponent_bits}, mantissa_bits={self.mantissa_bits}, "
            f"rounding_mode={self.rounding_mode})"
        )

    def close(self) -> None:
        if self._handle:
            self._library._lib.overfloat_spec_free(self._handle)
            self._handle = 0

    def __del__(self) -> None:
        self.close()


class OverfloatLibrary:
    def __init__(self, library_path: str | Path | None = None) -> None:
        if library_path is None:
            library_path = self._default_library_path()
        self._lib = CDLL(str(library_path))
        self._configure()

    def _configure(self) -> None:
        self._lib.overfloat_version_major.restype = c_int
        self._lib.overfloat_version_minor.restype = c_int
        self._lib.overfloat_version_patch.restype = c_int

        self._lib.overfloat_spec_create.argtypes = [c_int, c_int, c_int]
        self._lib.overfloat_spec_create.restype = c_void_p
        self._lib.overfloat_spec_free.argtypes = [c_void_p]
        self._lib.overfloat_spec_free.restype = None
        self._lib.overfloat_spec_exponent_bits.argtypes = [c_void_p]
        self._lib.overfloat_spec_exponent_bits.restype = c_int
        self._lib.overfloat_spec_mantissa_bits.argtypes = [c_void_p]
        self._lib.overfloat_spec_mantissa_bits.restype = c_int
        self._lib.overfloat_spec_rounding_mode.argtypes = [c_void_p]
        self._lib.overfloat_spec_rounding_mode.restype = c_int
        self._lib.overfloat_spec_validate.argtypes = [c_void_p]
        self._lib.overfloat_spec_validate.restype = c_int

        self._lib.overfloat_number_parse.argtypes = [c_void_p, c_char_p]
        self._lib.overfloat_number_parse.restype = c_void_p
        self._lib.overfloat_number_free.argtypes = [c_void_p]
        self._lib.overfloat_number_free.restype = None
        self._lib.overfloat_number_add.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_add.restype = c_void_p
        self._lib.overfloat_number_subtract.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_subtract.restype = c_void_p
        self._lib.overfloat_number_multiply.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_multiply.restype = c_void_p
        self._lib.overfloat_number_divide.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_divide.restype = c_void_p
        self._lib.overfloat_number_classification.argtypes = [c_void_p]
        self._lib.overfloat_number_classification.restype = c_int
        self._lib.overfloat_number_is_negative.argtypes = [c_void_p]
        self._lib.overfloat_number_is_negative.restype = c_int
        self._lib.overfloat_number_binary_exponent.argtypes = [c_void_p]
        self._lib.overfloat_number_binary_exponent.restype = c_int
        self._lib.overfloat_number_format.argtypes = [c_void_p, POINTER(c_char), c_int]
        self._lib.overfloat_number_format.restype = c_int

    def _default_library_path(self) -> Path:
        package_dir = Path(__file__).resolve().parent
        candidates = [
            package_dir / "liboverfloat.so",
            package_dir / "overfloat.dll",
            package_dir / "liboverfloat.dylib",
        ]
        for candidate in candidates:
            if candidate.exists():
                return candidate
        raise FileNotFoundError("Could not locate the bundled Overfloat native library.")

    def _wrap_number(self, handle: int, specification: OverfloatSpec) -> OverfloatNumber:
        if not handle:
            raise OverfloatError("Native operation failed.")
        return OverfloatNumber(self, int(handle), specification)

    @property
    def version(self) -> tuple[int, int, int]:
        return (
            int(self._lib.overfloat_version_major()),
            int(self._lib.overfloat_version_minor()),
            int(self._lib.overfloat_version_patch()),
        )

    def create_spec(self, exponent_bits: int, mantissa_bits: int, rounding_mode: int = 0) -> OverfloatSpec:
        handle = self._lib.overfloat_spec_create(exponent_bits, mantissa_bits, rounding_mode)
        if not handle:
            raise OverfloatError("Unable to create specification.")
        return OverfloatSpec(self, int(handle))

    def create_spec_from_total_bits(self, total_bits: int, rounding_mode: int = 0) -> OverfloatSpec:
        exponent_bits, mantissa_bits = self._resolve_standard_bit_widths(total_bits)
        return self.create_spec(exponent_bits, mantissa_bits, rounding_mode)

    @staticmethod
    def _resolve_standard_bit_widths(total_bits: int) -> tuple[int, int]:
        fixed_sizes = {
            16: (5, 10),
            32: (8, 23),
            64: (11, 52),
            128: (15, 112),
        }
        if total_bits in fixed_sizes:
            return fixed_sizes[total_bits]
        if total_bits < 16 or total_bits < 128:
            raise OverfloatError(
                "This total bit width has no standard definition. Use create_spec(exponent_bits, mantissa_bits) to specify widths manually."
            )
        if total_bits % 32 != 0:
            raise OverfloatError(
                "Total bit width must be a multiple of 32 for IEEE 754-2008 binary interchange format extensions greater than 128 bits."
            )

        exponent_bits = round(4 * math.log2(total_bits)) - 13
        mantissa_bits = total_bits - exponent_bits - 1
        if exponent_bits < 2 or mantissa_bits < 1:
            raise OverfloatError(
                "Unable to derive IEEE 754-2008 binary interchange format widths for the specified total bit width."
            )
        return exponent_bits, mantissa_bits
