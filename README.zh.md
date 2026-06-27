# Overfloat

Overfloat 是一个基于 .NET 8 Native AOT 开发的浮点数库，提供原生 C ABI 接口以及 Python 绑定层。这是一个面向 IEEE 754 标准的任意精度浮点数库。

## 项目状态 (Status)

目前已实现的功能支持：

- 自定义指数（exponent）和尾数（mantissa）宽度
- 将十进制字符串解析为量化后的二进制浮点值
- 零（zero）、次正规数（subnormal）、正规数（normal）、无穷大（infinity）及 NaN 的分类
- 加法、减法、乘法和除法运算
- 对量化结果进行精确的十进制格式化
- 导出原生 C ABI
- 通过 `ctypes` 加载原生库的 Python 绑定

## 仓库结构 (Repository Layout)

- `src/Overfloat` - 核心 .NET 实现及原生导出层
- `include` - 供原生消费者使用的公共 C 头文件
- `python` - Python 包及其打包元数据
- `tests` - .NET 测试项目
- `docs` - 架构与设计说明
- `.github/workflows` - CI 与发布自动化
- `VERSION` - 项目版本号的单一来源

## 构建要求 (Build Requirements)

- .NET 8 SDK
- .NET Native AOT 支持的平台工具链

使用 `dotnet publish` 命令配合相应的运行时标识符（RID）构建原生动态库，例如：

```powershell README.md
dotnet publish .\Overfloat.csproj -c Release -r win-x64
```

典型的输出路径如下：

- Windows: `bin/Release/net8.0/win-x64/publish/Overfloat.dll`
- Linux: `bin/Release/net8.0/linux-x64/publish/libOverfloat.so`
- macOS: `bin/Release/net8.0/osx-x64/publish/libOverfloat.dylib`

如果希望在不显式指定库路径的情况下使用 Python 包，请将生成的原生库放置在 `python/overfloat/` 目录下，并使用以下支持的文件名之一：

- `overfloat.dll`
- `liboverfloat.so`
- `liboverfloat.dylib`

## .NET 示例

```csharp README.md
using Overfloat;

// 创建规格：8位指数，23位尾数，舍入模式为就近舍入（偶数优先）
var spec = new OverfloatSpecification(8, 23, OverfloatRoundingMode.ToNearestEven);

var a = OverfloatNumber.Parse(spec, "1.5");
var b = OverfloatNumber.Parse(spec, "2.25");

Console.WriteLine(OverfloatMath.Add(a, b));
Console.WriteLine(OverfloatMath.Multiply(a, b));
Console.WriteLine(OverfloatMath.Divide(a, b));
```

## Python

### 安装

优先使用 GitHub Releases 中附带的 wheel。

每个 release 可以包含：

- 对应平台的 wheel
- 对应平台的原生库产物

下载与当前平台和 Python 版本匹配的 wheel，然后用 `pip` 安装。

当前 release workflow 生成的是 Python 3.11 wheel。

常见文件名示例：

- Windows：`overfloat-<version>-cp311-cp311-win_amd64.whl`
- Linux：`overfloat-<version>-cp311-cp311-manylinux_..._x86_64.whl`
- macOS：`overfloat-<version>-cp311-cp311-macosx_11_0_arm64.whl`

示例：

```powershell README.md
python -m pip install .\overfloat-<version>-cp311-cp311-win_amd64.whl
```

### 使用

安装完成后，直接导入包并调用 `OverfloatLibrary()`。

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)  # 32位浮点数，FP32

a = spec("1.5")
b = spec("2.25")

print(a + b)
print(a * b)
print(spec("1") / spec("10"))
```

`OverfloatLibrary()` 会在包目录里查找内置原生库，查找顺序如下：

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

示例：

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(4096)

a = spec("1.5")
b = spec("2.25")

print(lib.version)
print(spec.exponent_bits)
print(spec.mantissa_bits)
print(a + b)
print(a * b)
print(spec("1") / spec("10"))
print(a.to_bits_hex())
print(spec.from_bits_hex(a.to_bits_hex()))
print(a.compare(b))
print(a.compare_total(b))
print(lib.exception_flags)
```

如果需要显式管理对象生命周期：

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(4096) as spec:
    with spec("1.5") as value:
        print(value)
```

对于偏好显式解析语法的代码，`spec.parse("1.5")` 依然可用。

look! that's so easy!

### 直接使用已发布的原生库

这种方式适合在打包成 wheel 之前，先检查刚发布出来的原生库是否可用。

1. 构建或下载已发布的原生库。
2. 进入仓库的 `python/` 目录。
3. 使用显式路径加载原生库。

示例：

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

### 手动构建 wheel

1. 为目标平台构建原生库。
2. 将发布出来的原生库复制到 `python/overfloat/`。
3. 进入 `python/` 目录。
4. 运行 `python -m build --wheel --outdir dist .`。
5. 从 `python/dist/` 安装生成的 wheel。

Windows 示例：

```powershell README.md
dotnet publish .\Overfloat.csproj -c Release -r win-x64
Copy-Item .\bin\Release\net8.0\win-x64\publish\Overfloat.dll .\python\overfloat\overfloat.dll -Force
Set-Location .\python
python -m build --wheel --outdir dist .
python -m pip install .\dist\overfloat-<version>-cp311-cp311-win_amd64.whl
```

### 人工验证

安装 release wheel 后，或者显式加载已发布原生库后，可以先做几条快速检查。

基础算术检查：

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); print(spec('1.5') + spec('2.25'))"
```

预期输出：

```text
3.75
```

bit pattern 往返检查：

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); a = spec('1.5'); bits = a.to_bits_hex(); print(bits); print(spec.from_bits_hex(bits))"
```

负零检查：

```powershell README.md
python -c "from overfloat import OverfloatLibrary; lib = OverfloatLibrary(); spec = lib.create_spec_from_total_bits(32); n = spec.from_bits_hex('80000000'); print(str(n)); print(n.to_bits_hex())"
```

## C ABI 说明

C 接口使用不透明句柄（opaque handles）。创建规格（specification）或数值（number）的函数会将所有权转移给调用者，当不再需要这些值时，必须调用对应的 `*_free` 函数。

公共头文件位于 [`include/overfloat.h`](include/overfloat.h)。

## 舍入模式 (Rounding Modes)

`OverfloatRoundingMode` 的取值：

- `0` - `ToNearestEven`（就近舍入，偶数优先）
- `1` - `TowardZero`（向零舍入）
- `2` - `TowardPositiveInfinity`（向正无穷舍入）
- `3` - `TowardNegativeInfinity`（向负无穷舍入）
- `4` - `AwayFromZero`（背离零舍入）

## 测试 (Tests)

```powershell README.md
dotnet test .\tests\Overfloat.Tests\Overfloat.Tests.csproj -c Release
```

运行 Python 示例和临时 Python 检查时，请使用仓库的 `python/` 目录作为当前工作目录。

## 开源协议 (License)

Apache License 2.0.
