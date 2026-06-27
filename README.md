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

Prefer the wheel files attached to GitHub Releases. Each release can include platform-specific wheels and native library artifacts.

Install the wheel that matches the current platform and Python version. The current release workflow builds Python 3.11 wheels.

Examples:

- Windows: `overfloat-<version>-cp311-cp311-win_amd64.whl`
- Linux: `overfloat-<version>-cp311-cp311-manylinux_..._x86_64.whl`
- macOS: `overfloat-<version>-cp311-cp311-macosx_11_0_arm64.whl`

Install from a downloaded wheel:

```powershell README.md
python -m pip install .\overfloat-<version>-cp311-cp311-win_amd64.whl
```

After installation, use the package directly:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

print(spec("1.5") + spec("2.25"))
```

When `OverfloatLibrary()` is called without arguments, the wrapper looks for the native library next to the Python package in `overfloat/`. It checks these filenames in order:

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

When running from the repository `python/` directory, the default source-tree setup is:

- Python import: `from overfloat import OverfloatLibrary`
- Native library location: `python/overfloat/<platform library file>`

An explicit path remains supported for loading a library from another location.

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
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

### Preferred: use a release wheel

1. Open GitHub Releases.
2. Download the wheel that matches the current platform and Python version.
3. Install it with `python -m pip install <wheel-file>`.
4. Import `OverfloatLibrary` and call `OverfloatLibrary()` directly.

### Use a published native library directly

1. Build or download a published native library.
2. Change into the repository `python/` directory.
3. Load the library with an explicit path.

Example:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

This is useful for checking a freshly published native build before packaging it into a wheel.

### Build a wheel manually

1. Build the native library for the target platform.
2. Copy the published native library into `python/overfloat/`.
3. Change into the `python/` directory.
4. Run `python -m build --wheel --outdir dist .`
5. Install the generated wheel from `python/dist/`.

Windows example:

```powershell README.md
dotnet publish .\Overfloat.csproj -c Release -r win-x64
Copy-Item .\bin\Release\net8.0\win-x64\publish\Overfloat.dll .\python\overfloat\overfloat.dll -Force
Set-Location .\python
python -m build --wheel --outdir dist .
python -m pip install .\dist\overfloat-<version>-cp311-cp311-win_amd64.whl
```

### Day-to-day package usage

1. Use `create_spec_from_total_bits(total_bits)` for a ready-made FPxxx-style format.
2. Call the spec like a function to parse values.
3. Use normal Python operators for arithmetic.
4. Inspect `to_bits_hex()`, `from_bits_hex()`, `compare()`, and `compare_total()` for lower-level checks.
5. Read `exception_flags` after operations that may raise IEEE-style status bits.
6. Call `close()` for deterministic native-handle cleanup, or use `with` for explicit lifetime management.

Example:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
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

You can also manage object lifetime explicitly:

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(4096) as spec:
    with spec("1.5") as value:
        print(value)
```

`spec.parse("1.5")` remains available for code that prefers the explicit parsing form.

### Manual validation steps

For a quick manual check after installing a release wheel or loading a published native library explicitly:

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
