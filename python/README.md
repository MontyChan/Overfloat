# overfloat

Python bindings for the Overfloat native floating-point runtime. Overfloat is an IEEE 754-oriented arbitrary-precision floating-point library implemented as a .NET Native AOT shared library with a C ABI.

## Install

Install from PyPI:

```powershell
python -m pip install overfloat
```

The release workflow builds wheels for Python 3.9, 3.10, 3.11, 3.12, and 3.13. Downloaded wheels bundle the native Overfloat shared library for the target platform.

Typical wheel names:

- Windows: `overfloat-<version>-cp<python>-cp<python>-win_amd64.whl`
- Linux: `overfloat-<version>-cp<python>-cp<python>-manylinux_..._x86_64.whl`
- macOS: `overfloat-<version>-cp<python>-cp<python>-macosx_11_0_arm64.whl`

For example, Python 3.9 uses `cp39`, Python 3.10 uses `cp310`, Python 3.11 uses `cp311`, Python 3.12 uses `cp312`, and Python 3.13 uses `cp313`.

## Usage

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

a = spec("1.5")
b = spec("2.25")

print(a + b)
print(a * b)
print(spec("1") / spec("10"))
```

`OverfloatLibrary()` loads the bundled native library by default. You can also pass an explicit shared library path.

```python
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

These preset values match the IEEE 754 standard formats. For IEEE 754-2008 interchange-style formats with `k >= 128` and `k` a multiple of `32`, `create_spec_from_total_bits(k)` follows the standard width derivation.

For explicit lifetime management:

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(4096) as spec:
    with spec("1.5") as value:
        print(value)
```

## Build From Source

Build or copy the platform native library into `overfloat/` before creating a wheel or loading the package with `OverfloatLibrary()`.

Windows example:

```powershell
dotnet publish .\Overfloat.csproj -c Release -r win-x64
Copy-Item .\bin\Release\net8.0\win-x64\publish\Overfloat.dll .\python\overfloat\overfloat.dll -Force
Set-Location .\python
python -m build --wheel --outdir dist .
```

## License

Apache-2.0
