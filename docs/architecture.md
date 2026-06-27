# Overfloat Architecture

## Contents

- Overview
- Current Components
- Numeric Model
- Interop Model
- Python Binding API
- `OverfloatLibrary`
- `OverfloatSpec`
- `OverfloatNumber`
- Error Model
- Object Lifetime
- Typical Flow
- Compatibility Notes

## Overview

Overfloat is organized around a single core implementation that is exposed through a native C ABI and consumed from Python through `ctypes`.

The repository is intentionally arranged so the numeric core, the ABI surface, and the packaging layer can evolve independently while staying aligned on the same behavior.

## Current Components

- Core numeric model and arithmetic implementation in `src/Overfloat`
- Native export layer using `[UnmanagedCallersOnly]`
- Public C header in `include/overfloat.h`
- Python wrapper in `python/overfloat`
- .NET test project in `tests/Overfloat.Tests`
- CI and release workflows in `.github/workflows`

## Numeric Model

The current runtime represents values as a quantized floating-point number with:

- sign
- exponent
- significand
- classification: zero, subnormal, normal, infinity, NaN
- rounding mode

The model is sufficient for the current arithmetic slice.

## Interop Model

### C ABI

The C surface is built around opaque handles. Callers create a specification, parse or compute numbers, and release handles explicitly when finished. This keeps the boundary small and avoids exposing managed implementation details.

### Python Layer

The Python package loads the platform native library with `ctypes` and maps the C ABI to Python objects. The wrapper currently favors a small object-oriented API over direct function calls.

## Python Binding API

The Python package lives in `python/overfloat` and exposes three main public classes:

- `OverfloatLibrary`
- `OverfloatSpec`
- `OverfloatNumber`

The package also exposes one public exception type:

- `OverfloatError`

### Loading The Library

`OverfloatLibrary` is the Python entry point.

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
```

If no path is passed, `OverfloatLibrary()` looks for a bundled native library in the package directory and checks these filenames in order:

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

An explicit path is also supported:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

### `OverfloatLibrary`

Main responsibilities:

- load the native library through `ctypes`
- expose package version information
- expose the current exception flags
- create `OverfloatSpec` objects

Public members:

- `OverfloatLibrary(library_path: str | Path | None = None)`
- `version -> tuple[int, int, int]`
- `exception_flags -> int`
- `clear_exception_flags() -> None`
- `create_spec(exponent_bits: int, mantissa_bits: int, rounding_mode: int = 0) -> OverfloatSpec`
- `create_spec_from_total_bits(total_bits: int, rounding_mode: int = 0) -> OverfloatSpec`

Typical usage:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

print(lib.version)
print(lib.exception_flags)
```

### `OverfloatSpec`

`OverfloatSpec` represents one floating-point format definition.

There are two ways to create a spec from Python:

- `create_spec_from_total_bits(total_bits)` for built-in and standard-derived formats
- `create_spec(exponent_bits, mantissa_bits)` for manual width selection

Built-in preset formats:

- `16` -> exponent `5`, mantissa `10`
- `32` -> exponent `8`, mantissa `23`
- `64` -> exponent `11`, mantissa `52`
- `128` -> exponent `15`, mantissa `112`

These preset values also match the IEEE 754 standard formats. The implementation keeps them as direct built-in mappings.

For IEEE 754-2008 interchange-style formats with `k >= 128` and `k` a multiple of `32`, the total-bit form uses this derivation:

```text
k = total bit width, including the sign bit
w = round(4 × log2(k)) - 13    exponent width
t = k - w - 1                  mantissa storage width, excluding the hidden bit
p = t + 1 = k - w              precision, including the hidden bit
```

Public properties:

- `exponent_bits -> int`
- `mantissa_bits -> int`
- `rounding_mode -> int`

Public methods:

- `parse(text: str) -> OverfloatNumber`
- `from_bits_hex(hex_text: str) -> OverfloatNumber`
- `number(value: OverfloatNumber | str | int | float) -> OverfloatNumber`
- `close() -> None`

Supported convenience behavior:

- `spec(value)` is the same as `spec.number(value)`
- `with spec:` is supported for explicit cleanup

Typical usage:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

a = spec("1.5")
b = spec.parse("2.25")
c = spec.number(10)

print(spec.exponent_bits)
print(spec.mantissa_bits)
print(spec.rounding_mode)
print(spec.from_bits_hex(a.to_bits_hex()))
```

Type coercion rules in `number(...)`:

- `OverfloatNumber`: returned as-is if it belongs to the same spec
- `str`: parsed directly
- `bool`: converted to `0` or `1`, then parsed
- `int` and `float`: converted with `repr(value)`, then parsed

### `OverfloatNumber`

`OverfloatNumber` wraps one native floating-point value.

Public properties:

- `specification -> OverfloatSpec`
- `classification -> int`
- `is_negative -> bool`
- `binary_exponent -> int`
- `is_signaling_nan -> bool`
- `nan_payload -> int`

Public methods:

- `to_bits_hex() -> str`
- `compare(other) -> int`
- `compare_total(other) -> int`
- `add(other) -> OverfloatNumber`
- `subtract(other) -> OverfloatNumber`
- `multiply(other) -> OverfloatNumber`
- `divide(other) -> OverfloatNumber`
- `close() -> None`

Supported operators and conversions:

- `str(number)` for decimal formatting
- `number + other`
- `number - other`
- `number * other`
- `number / other`
- unary `+number`
- unary `-number`
- `==`, `<`, `<=`, `>`, `>=`
- `with number:` for explicit cleanup

Typical usage:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

a = spec("1.5")
b = spec("2.25")

print(a + b)
print(a * b)
print(spec("1") / spec("10"))
print(a.to_bits_hex())
print(a.compare(b))
print(a.compare_total(b))
print(a.classification)
print(a.is_negative)
```

### Error Model

The wrapper raises `OverfloatError` for native-operation failures, including:

- invalid compare operations with NaN in ordered comparisons
- native parse or arithmetic failures
- string formatting failures
- invalid native handle results

`TypeError` is raised for unsupported Python input types passed into `OverfloatSpec.number(...)`.

### Object Lifetime

The Python API uses explicit native handles under the hood.

Cleanup options:

- call `close()` on `OverfloatSpec` and `OverfloatNumber`
- use `with` blocks for deterministic cleanup
- rely on `__del__` only as a fallback

Recommended pattern:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(32) as spec:
    with spec("1.5") as a:
        with spec("2.25") as b:
            print(a + b)
```

### Typical Flow

The normal Python call flow is:

1. Create `OverfloatLibrary`
2. Create an `OverfloatSpec`
3. Parse or coerce values into `OverfloatNumber`
4. Run arithmetic or comparison operations
5. Inspect formatted output, bit patterns, or exception flags
6. Release native handles explicitly when deterministic cleanup is needed

## Compatibility Notes

- The public C ABI should be treated as a compatibility boundary.
- Native library filenames and wheel contents are part of the packaging contract.
- Changes that alter numeric rounding or formatting behavior should be documented and tested explicitly.
