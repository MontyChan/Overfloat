from __future__ import annotations

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

    @property
    def is_signaling_nan(self) -> bool:
        return bool(self._library._lib.overfloat_number_is_signaling_nan(self._handle))

    @property
    def nan_payload(self) -> int:
        payload = self._library._call_string_function(self._library._lib.overfloat_number_nan_payload_format, self._handle)
        return int(payload) if payload else 0

    def to_bits_hex(self) -> str:
        return self._library._call_string_function(self._library._lib.overfloat_number_to_bits_hex, self._handle)

    def compare(self, other: NumberLike) -> int:
        coerced = self._coerce_other(other)
        result = int(self._library._lib.overfloat_number_compare(self._handle, coerced._handle))
        if result == 2:
            raise OverfloatError("Ordered comparison is undefined for NaN operands.")
        if result == -2147483648:
            raise OverfloatError("Native comparison failed.")
        return result

    def compare_total(self, other: NumberLike) -> int:
        coerced = self._coerce_other(other)
        result = int(self._library._lib.overfloat_number_compare_total(self._handle, coerced._handle))
        if result == -2147483648:
            raise OverfloatError("Native total-order comparison failed.")
        return result

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
        try:
            return self.compare(other) == 0
        except OverfloatError:
            return False

    def __lt__(self, other: NumberLike) -> bool:
        try:
            return self.compare(other) < 0
        except OverfloatError:
            return False

    def __le__(self, other: NumberLike) -> bool:
        try:
            return self.compare(other) <= 0
        except OverfloatError:
            return False

    def __gt__(self, other: NumberLike) -> bool:
        try:
            return self.compare(other) > 0
        except OverfloatError:
            return False

    def __ge__(self, other: NumberLike) -> bool:
        try:
            return self.compare(other) >= 0
        except OverfloatError:
            return False

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

    def from_bits_hex(self, hex_text: str) -> OverfloatNumber:
        handle = self._library._lib.overfloat_number_from_bits_hex(self._handle, hex_text.encode("utf-8"))
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
        self._lib.overfloat_exception_flags_get.restype = c_int
        self._lib.overfloat_exception_flags_clear.restype = None

        self._lib.overfloat_spec_create.argtypes = [c_int, c_int, c_int]
        self._lib.overfloat_spec_create.restype = c_void_p
        self._lib.overfloat_spec_create_from_total_bits.argtypes = [c_int, c_int]
        self._lib.overfloat_spec_create_from_total_bits.restype = c_void_p
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
        self._lib.overfloat_number_from_bits_hex.argtypes = [c_void_p, c_char_p]
        self._lib.overfloat_number_from_bits_hex.restype = c_void_p
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
        self._lib.overfloat_number_compare.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_compare.restype = c_int
        self._lib.overfloat_number_compare_total.argtypes = [c_void_p, c_void_p]
        self._lib.overfloat_number_compare_total.restype = c_int
        self._lib.overfloat_number_classification.argtypes = [c_void_p]
        self._lib.overfloat_number_classification.restype = c_int
        self._lib.overfloat_number_is_negative.argtypes = [c_void_p]
        self._lib.overfloat_number_is_negative.restype = c_int
        self._lib.overfloat_number_is_signaling_nan.argtypes = [c_void_p]
        self._lib.overfloat_number_is_signaling_nan.restype = c_int
        self._lib.overfloat_number_binary_exponent.argtypes = [c_void_p]
        self._lib.overfloat_number_binary_exponent.restype = c_int
        self._lib.overfloat_number_format.argtypes = [c_void_p, POINTER(c_char), c_int]
        self._lib.overfloat_number_format.restype = c_int
        self._lib.overfloat_number_nan_payload_format.argtypes = [c_void_p, POINTER(c_char), c_int]
        self._lib.overfloat_number_nan_payload_format.restype = c_int
        self._lib.overfloat_number_to_bits_hex.argtypes = [c_void_p, POINTER(c_char), c_int]
        self._lib.overfloat_number_to_bits_hex.restype = c_int

    def _call_string_function(self, function, handle: int) -> str:
        required = int(function(handle, None, 0))
        if required <= 0:
            return ""
        buffer = create_string_buffer(required)
        written = int(function(handle, buffer, len(buffer)))
        if written <= 0:
            raise OverfloatError("Native string operation failed.")
        return buffer.value.decode("utf-8")

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

    @property
    def exception_flags(self) -> int:
        return int(self._lib.overfloat_exception_flags_get())

    def clear_exception_flags(self) -> None:
        self._lib.overfloat_exception_flags_clear()

    def create_spec(self, exponent_bits: int, mantissa_bits: int, rounding_mode: int = 0) -> OverfloatSpec:
        handle = self._lib.overfloat_spec_create(exponent_bits, mantissa_bits, rounding_mode)
        if not handle:
            raise OverfloatError("Unable to create specification.")
        return OverfloatSpec(self, int(handle))

    def create_spec_from_total_bits(self, total_bits: int, rounding_mode: int = 0) -> OverfloatSpec:
        handle = self._lib.overfloat_spec_create_from_total_bits(total_bits, rounding_mode)
        if not handle:
            raise OverfloatError("Unable to create specification from total bit width.")
        return OverfloatSpec(self, int(handle))


