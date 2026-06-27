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

## Python 示例

这些示例应在仓库的 `python/` 目录中运行，这样可以直接导入包，而不必手动修改 `sys.path`。

当 `OverfloatLibrary()` 不传参数直接调用时，封装层会到 Python 包旁边的 `python/overfloat/` 目录查找原生库。查找顺序如下：

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

在仓库的 `python/` 目录中运行时，默认约定如下：

- Python 导入写法：`from overfloat import OverfloatLibrary`
- 原生库位置：`python/overfloat/<当前平台的库文件>`

从其他位置加载原生库时，仍然可以显式传入路径。

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

# FP16384，采用本库使用的IEEE 754导向格式。
# 也可手动指定指数和尾数的位数。

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

## Python 教程

1. 先进入仓库的 `python/` 目录。
2. 构建原生库或将其复制到 `python/overfloat/` 目录下，这样封装层即可自动加载。
3. 导入 `OverfloatLibrary` 并创建一个库实例。
4. 需要标准的 FPxxx 格式时，使用 `create_spec_from_total_bits(total_bits)`。
5. 像调用函数一样调用 `spec` 对象来解析数值。
6. 使用标准的 Python 运算符进行算术运算。
7. 需要底层检查时，可以使用 `to_bits_hex()`、`from_bits_hex()`、`compare()` 和 `compare_total()`。
8. 在可能引发 IEEE 状态位的操作后，检查 `exception_flags`。
9. 需要确定性的原生句柄释放时，可调用 `close()`，或者使用 `with` 管理对象生命周期。

如果原生库不放在 `python/overfloat/` 中，也仍然可以显式指定路径，例如：

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

示例：

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

也可以显式管理对象生命周期：

```python README.md
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(4096) as spec:
    with spec("1.5") as value:
        print(value)
```

对于偏好显式解析语法的代码，`spec.parse("1.5")` 依然可用。

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
