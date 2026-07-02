from pathlib import Path

from setuptools import Distribution, setup


class BinaryDistribution(Distribution):
    def has_ext_modules(self) -> bool:
        return True


def read_version() -> str:
    root_version = Path(__file__).resolve().parent.parent / "VERSION"
    if root_version.exists():
        return root_version.read_text(encoding="utf-8").strip()

    pkg_info = Path(__file__).resolve().parent / "PKG-INFO"
    if pkg_info.exists():
        for line in pkg_info.read_text(encoding="utf-8").splitlines():
            if line.startswith("Version: "):
                return line.removeprefix("Version: ").strip()

    raise RuntimeError("Unable to determine package version.")


setup(distclass=BinaryDistribution, version=read_version())
