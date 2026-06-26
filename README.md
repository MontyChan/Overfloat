# Overfloat

Overfloat is a .NET 8 Native AOT floating-point library with a native C ABI and a Python binding layer. It is an IEEE 754-oriented arbitrary-precision floating-point library.

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
spec = lib.create_spec(8, 23)

a = spec("1.5")
b = spec("2.25")

print(lib.version)
print(spec.exponent_bits)
print(spec.mantissa_bits)
print(a + b)
print(a * b)
print(spec("1") / spec("10"))
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
