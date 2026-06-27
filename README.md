# Overfloat

Overfloat is a .NET 8 Native AOT floating-point library with a native C ABI and a Python binding layer. It is an IEEE 754-oriented arbitrary-precision floating-point library.

[简体中文](./README.zh.md) | [English](./README.md)

## Status

The implementation currently supports:

- custom exponent and mantissa widths
- decimal parsing into quantized binary floating-point values
- zero, subnormal, normal, infinity, and NaN classification
- addition, subtraction, multiplication, and division
- exact decimal formatting of quantized results
- native C ABI exports
- Python bindings that load the native library through `ctypes`

## Repository Layout

- `src/Overfloat` - core .NET implementation and native export layer
- `include` - public C header for native consumers
- `python` - Python package and packaging metadata
- `tests` - .NET test project
- `docs` - architecture and design notes
- `.github/workflows` - CI and release automation

## Build Requirements

- .NET 8 SDK
- a platform toolchain supported by .NET Native AOT

Build a native shared library with `dotnet publish` and an appropriate runtime identifier, for example:

```powershell README.md
dotnet publish .\Overfloat.csproj -c Release -r win-x64
```

Representative output paths are:

- Windows: `bin/Release/net8.0/win-x64/publish/Overfloat.dll`
- Linux: `bin/Release/net8.0/linux-x64/publish/libOverfloat.so`
- macOS: `bin/Release/net8.0/osx-x64/publish/libOverfloat.dylib`

To use the Python package without an explicit library path, place the published native library in `python/overfloat/` under one of the supported filenames:

- `overfloat.dll`
- `liboverfloat.so`
- `liboverfloat.dylib`

## .NET Example

```csharp README.md
using Overfloat;

var spec = new OverfloatSpecification(8, 23, OverfloatRoundingMode.ToNearestEven);

var a = OverfloatNumber.Parse(spec, "1.5");
var b = OverfloatNumber.Parse(spec, "2.25");

Console.WriteLine(OverfloatMath.Add(a, b));
Console.WriteLine(OverfloatMath.Multiply(a, b));
Console.WriteLine(OverfloatMath.Divide(a, b));
```

## Python Example

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("python/overfloat/overfloat.dll")
# FP16384, in the IEEE 754-oriented format used by this library.
# You can also spell the format out manually with exponent and mantissa bits.
spec = lib.create_spec_from_total_bits(16384)
manual_spec = lib.create_spec(43, 16340)

a = spec("1.5")
b = spec("2.25")

print(lib.version)
print(spec.exponent_bits)
print(spec.mantissa_bits)
print(a + b)
print(a * b)
print(spec("1") / spec("10"))
print(manual_spec.exponent_bits)
print(manual_spec.mantissa_bits)
```

look! that's so easy!

## Python Tutorial

1. Build or copy the native library into `python/overfloat/` so the wrapper can load it.
2. Import `OverfloatLibrary` and create a library instance.
3. Use `create_spec_from_total_bits(total_bits)` when you want a ready-made FPxxx-style format.
4. Call the spec like a function to parse values.
5. Use normal Python operators for arithmetic.
6. Inspect `to_bits_hex()`, `from_bits_hex()`, `compare()`, and `compare_total()` when you need lower-level checks.
7. Read `exception_flags` after operations that may raise IEEE-style status bits.

Example:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("python/overfloat/overfloat.dll")
spec = lib.create_spec_from_total_bits(4096)

a = spec("1.5")
b = spec("2.25")

print(spec.exponent_bits)
print(spec.mantissa_bits)
print(a + b)
print(a.to_bits_hex())
print(spec.from_bits_hex(a.to_bits_hex()))
print(a.compare(b))
print(a.compare_total(b))
print(lib.exception_flags)
```

`spec.parse("1.5")` remains available for code that prefers the explicit parsing form.

## C ABI Notes

The C interface uses opaque handles. Functions that create a specification or number transfer ownership to the caller, and the matching `*_free` function must be called when the value is no longer needed.

The public header is [`include/overfloat.h`](include/overfloat.h).

## Rounding Modes

`OverfloatRoundingMode` values:

- `0` - `ToNearestEven`
- `1` - `TowardZero`
- `2` - `TowardPositiveInfinity`
- `3` - `TowardNegativeInfinity`
- `4` - `AwayFromZero`

## Tests

```powershell README.md
dotnet run --project .\tests\Overfloat.Tests\Overfloat.Tests.csproj -c Release
```

## License

Apache License 2.0.
