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
- `VERSION` - single source of truth for the project version

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

## Python

### Install

Prefer the wheel files attached to GitHub Releases.

Each release can contain:

- platform-specific wheels
- platform-specific native library artifacts

Download the wheel that matches the current platform and Python version, then install it with `pip`.

The current release workflow builds Python 3.11 wheels.

Typical wheel names:

- Windows: `overfloat-<version>-cp311-cp311-win_amd64.whl`
- Linux: `overfloat-<version>-cp311-cp311-manylinux_..._x86_64.whl`
- macOS: `overfloat-<version>-cp311-cp311-macosx_11_0_arm64.whl`

Example:

```powershell README.md
python -m pip install .\overfloat-<version>-cp311-cp311-win_amd64.whl
```

### Use

After installation, import the package and call `OverfloatLibrary()` directly.

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)  # 32-bit floating-point format, FP32

a = spec("1.5")
b = spec("2.25")

print(a + b)
print(a * b)
print(spec("1") / spec("10"))
```

If no path is passed, `OverfloatLibrary()` looks for the native library file that is installed alongside the Python package. When the package is installed from a wheel, that library file is already bundled inside the package. It checks these filenames in order:

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

Example:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(4096)
manual_spec = lib.create_spec(43, 4048)

a = spec("1.5")
b = spec("2.25")
manual_value = manual_spec("1.5")

print(lib.version)
print(spec.exponent_bits)
print(spec.mantissa_bits)
print(a + b)
print(a * b)
print(spec("1") / spec("10"))
print(manual_spec.exponent_bits)
print(manual_spec.mantissa_bits)
print(manual_value)
print(a.to_bits_hex())
print(spec.from_bits_hex(a.to_bits_hex()))
print(a.compare(b))
print(a.compare_total(b))
print(lib.exception_flags)
```

`create_spec_from_total_bits(total_bits)` is the convenient preset form.

`create_spec(exponent_bits, mantissa_bits)` is the manual form when the exponent width and mantissa width need to be set directly.

Built-in preset formats:

- `16` -> exponent `5`, mantissa `10`
- `32` -> exponent `8`, mantissa `23`
- `64` -> exponent `11`, mantissa `52`
- `128` -> exponent `15`, mantissa `112`

These preset values also match the IEEE 754 standard formats. The implementation handles them as direct built-in mappings instead of recomputing them from the formula.

For IEEE 754-2008 interchange-style formats with `k >= 128` and `k` a multiple of `32`, `create_spec_from_total_bits(k)` follows the standard width derivation:

```text
k = total bit width, including the sign bit
w = round(4 × log2(k)) - 13    exponent width
t = k - w - 1                  mantissa storage width, excluding the hidden bit
p = t + 1 = k - w              precision, including the hidden bit
```

For explicit lifetime management:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(4096) as spec:
    with spec("1.5") as value:
        print(value)
```

`spec.parse("1.5")` remains available for code that prefers the explicit parsing form.

look! that's so easy!

### Use A Published Native Library Directly

This path is useful for checking a freshly published native build before packaging it into a wheel.

1. Build or download a published native library.
2. Change into the repository `python/` directory.
3. Load the library with an explicit path.

Example:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

### Build A Wheel Manually

1. Build the native library for the target platform.
2. Copy the published native library into `python/overfloat/`.
3. Change into the `python/` directory.
4. Run `python -m build --wheel --outdir dist .`.
5. Install the generated wheel from `python/dist/`.

Windows example:

```powershell README.md
dotnet publish .\Overfloat.csproj -c Release -r win-x64
Copy-Item .\bin\Release\net8.0\win-x64\publish\Overfloat.dll .\python\overfloat\overfloat.dll -Force
Set-Location .\python
python -m build --wheel --outdir dist .
python -m pip install .\dist\overfloat-<version>-cp311-cp311-win_amd64.whl
```

### Manual Validation

After installing a release wheel, or after loading a published native library explicitly, run a few quick checks.

Basic arithmetic check:

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); print(spec('1.5') + spec('2.25'))"
```

Expected output:

```text
3.75
```

Bit-pattern round-trip check:

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); a = spec('1.5'); bits = a.to_bits_hex(); print(bits); print(spec.from_bits_hex(bits))"
```

Negative-zero check:

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); n = spec.from_bits_hex('80000000'); print(str(n)); print(n.to_bits_hex())"
```

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
dotnet test .\tests\Overfloat.Tests\Overfloat.Tests.csproj -c Release
```

Run Python examples and ad hoc Python checks from the repository `python/` directory.

## License

Apache License 2.0.
