# Overfloat Architecture

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

The model is sufficient for the current arithmetic slice, but it is not yet a complete IEEE 754 implementation.

## Interop Model

### C ABI

The C surface is built around opaque handles. Callers create a specification, parse or compute numbers, and release handles explicitly when finished. This keeps the boundary small and avoids exposing managed implementation details.

### Python Layer

The Python package loads the platform native library with `ctypes` and maps the C ABI to Python objects. The wrapper currently favors a small object-oriented API over direct function calls.

## Compatibility Notes

- The public C ABI should be treated as a compatibility boundary.
- Native library filenames and wheel contents are part of the packaging contract.
- Changes that alter numeric rounding or formatting behavior should be documented and tested explicitly.

## Planned Work

The next major implementation steps are:

- raw bit-pattern encode and decode
- exception status and flags
- NaN payload support
- broader boundary and conformance coverage
- platform packaging automation for native artifacts and wheels
