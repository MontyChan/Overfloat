# Contributing to Overfloat

## Scope

Keep contributions narrow and reviewable. Prefer one logical change per pull request unless multiple edits are required to keep the repository consistent.

## Working Rules

1. Update tests with behavior changes.
2. Preserve the native ABI unless a change is explicitly part of an API revision.
3. Keep the Python layer thin and aligned with the C ABI.
4. Keep docs and examples synchronized with the exposed API.
5. Avoid introducing new packaging or runtime dependencies unless they are required.

## Code Standards

- Use .NET 8 and C# for the runtime and native export implementation.
- Keep interop code in the native layer; do not leak ABI concerns into the core numeric model.
- Treat public exported symbols as part of the compatibility surface.
- Keep repository structure stable: `src`, `tests`, `docs`, `include`, and `python`.
- Favor explicit names and simple control flow over abstraction when the implementation is still evolving.

## Testing Expectations

- Add or update unit tests for arithmetic behavior, parsing, formatting, and classification changes.
- Add focused regression coverage for rounding and boundary cases when touching quantization logic.
- Validate the Python wrapper when changes affect the native ABI or packaging layout.

## Packaging Expectations

- Native artifacts should remain platform-specific and self-contained.
- Wheels should bundle the native library for the target platform.
- Do not commit build outputs, generated caches, or local development binaries.

## Release Expectations

- Native builds should cover `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`.
- Release notes should call out ABI changes, packaging changes, and any behavior changes that affect numeric results.
- Changes to the public header should be reflected in the README and architecture notes.
