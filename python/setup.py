from pathlib import Path

from setuptools import Distribution, setup


class BinaryDistribution(Distribution):
    def has_ext_modules(self) -> bool:
        return True


def read_version() -> str:
    return (Path(__file__).resolve().parent.parent / "VERSION").read_text(encoding="utf-8").strip()


setup(distclass=BinaryDistribution, version=read_version())
