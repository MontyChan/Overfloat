# Overfloat 架构说明

## 目录

- 概览
- 当前组件
- 数值模型
- 互操作模型
- Python 绑定 API
- `OverfloatLibrary`
- `OverfloatSpec`
- `OverfloatNumber`
- 错误模型
- 对象生命周期
- 典型调用流程
- 兼容性说明

## 概览

Overfloat 以一套核心实现为中心，通过原生 C ABI 对外暴露，并通过 `ctypes` 提供 Python 绑定。

仓库结构的设计目标是让数值核心、ABI 边界和打包层可以分别演进，同时保持行为一致。

## 当前组件

- `src/Overfloat` 中的核心数值模型和算术实现
- 使用 `[UnmanagedCallersOnly]` 的原生导出层
- `include/overfloat.h` 中的公共 C 头文件
- `python/overfloat` 中的 Python 封装层
- `tests/Overfloat.Tests` 中的 .NET 测试项目
- `.github/workflows` 中的 CI 和发布工作流

## 数值模型

当前运行时用量化后的浮点数表示值，包含这些组成部分：

- 符号位
- 指数
- 有效数
- 分类：零、次正规数、正规数、无穷大、NaN
- 舍入模式

这套模型已经覆盖当前支持的算术范围。

## 互操作模型

### C ABI

C 接口围绕不透明句柄设计。调用方负责创建规格、解析或计算数值，并在使用结束后显式释放句柄。这样可以保持边界较小，同时避免暴露托管实现细节。

### Python 层

Python 包通过 `ctypes` 加载平台对应的原生库，并把 C ABI 映射成 Python 对象。当前封装更偏向小而直接的面向对象 API，而不是暴露原始函数调用。

## Python 绑定 API

Python 包位于 `python/overfloat`，公开的核心类型有三个：

- `OverfloatLibrary`
- `OverfloatSpec`
- `OverfloatNumber`

另外还公开一个异常类型：

- `OverfloatError`

### 加载原生库

`OverfloatLibrary` 是 Python 侧的入口。

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
```

如果不传路径，`OverfloatLibrary()` 会在 Python 包所在目录中查找随包一起安装的原生库文件。通过 wheel 安装时，这个库文件已经打包在包内。查找顺序如下：

- `liboverfloat.so`
- `overfloat.dll`
- `liboverfloat.dylib`

也可以显式传入路径：

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary("../bin/Release/net8.0/win-x64/publish/Overfloat.dll")
```

### `OverfloatLibrary`

主要职责：

- 通过 `ctypes` 加载原生库
- 提供版本信息
- 提供当前异常标志
- 创建 `OverfloatSpec` 对象

公开成员：

- `OverfloatLibrary(library_path: str | Path | None = None)`
- `version -> tuple[int, int, int]`
- `exception_flags -> int`
- `clear_exception_flags() -> None`
- `create_spec(exponent_bits: int, mantissa_bits: int, rounding_mode: int = 0) -> OverfloatSpec`
- `create_spec_from_total_bits(total_bits: int, rounding_mode: int = 0) -> OverfloatSpec`

典型用法：

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

print(lib.version)
print(lib.exception_flags)
```

### `OverfloatSpec`

`OverfloatSpec` 表示一种浮点格式定义。

Python 侧有两种创建 spec 的方式：

- `create_spec_from_total_bits(total_bits)`，用于内置预设格式和标准推导格式
- `create_spec(exponent_bits, mantissa_bits)`，用于手动指定位宽

内置预设格式：

- `16` -> 指数位 `5`，尾数位 `10`
- `32` -> 指数位 `8`，尾数位 `23`
- `64` -> 指数位 `11`，尾数位 `52`
- `128` -> 指数位 `15`，尾数位 `112`

这些预设值本身也符合 IEEE 754 标准格式。实现里保留了它们的直接内置映射。

对于符合 IEEE 754-2008 交换格式扩展规则的情况，也就是 `k >= 128` 且 `k` 是 `32` 的倍数时，总位宽形式使用下面这组推导公式：

```text
k = 总位数，包含符号位
w = round(4 × log2(k)) - 13    指数位宽
t = k - w - 1                  尾数存储位宽，不含隐藏位
p = t + 1 = k - w              精度，包含隐藏位
```

公开属性：

- `exponent_bits -> int`
- `mantissa_bits -> int`
- `rounding_mode -> int`

公开方法：

- `parse(text: str) -> OverfloatNumber`
- `from_bits_hex(hex_text: str) -> OverfloatNumber`
- `number(value: OverfloatNumber | str | int | float) -> OverfloatNumber`
- `close() -> None`

支持的便利行为：

- `spec(value)` 等同于 `spec.number(value)`
- 支持 `with spec:` 做显式清理

典型用法：

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

a = spec("1.5")
b = spec.parse("2.25")
c = spec.number(10)

print(spec.exponent_bits)
print(spec.mantissa_bits)
print(spec.rounding_mode)
print(spec.from_bits_hex(a.to_bits_hex()))
```

`number(...)` 的转换规则：

- `OverfloatNumber`：如果属于同一个 spec，则直接返回
- `str`：直接解析
- `bool`：先转成 `0` 或 `1`，再解析
- `int` 和 `float`：先用 `repr(value)` 转成字符串，再解析

### `OverfloatNumber`

`OverfloatNumber` 封装一个原生浮点值。

公开属性：

- `specification -> OverfloatSpec`
- `classification -> int`
- `is_negative -> bool`
- `binary_exponent -> int`
- `is_signaling_nan -> bool`
- `nan_payload -> int`

公开方法：

- `to_bits_hex() -> str`
- `compare(other) -> int`
- `compare_total(other) -> int`
- `add(other) -> OverfloatNumber`
- `subtract(other) -> OverfloatNumber`
- `multiply(other) -> OverfloatNumber`
- `divide(other) -> OverfloatNumber`
- `close() -> None`

支持的运算和转换：

- `str(number)` 用于十进制格式化
- `number + other`
- `number - other`
- `number * other`
- `number / other`
- 一元 `+number`
- 一元 `-number`
- `==`、`<`、`<=`、`>`、`>=`
- 支持 `with number:` 做显式清理

典型用法：

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()
spec = lib.create_spec_from_total_bits(32)

a = spec("1.5")
b = spec("2.25")

print(a + b)
print(a * b)
print(spec("1") / spec("10"))
print(a.to_bits_hex())
print(a.compare(b))
print(a.compare_total(b))
print(a.classification)
print(a.is_negative)
```

### 错误模型

以下情况会抛出 `OverfloatError`：

- 含 NaN 的有序比较
- 原生解析或算术操作失败
- 字符串格式化失败
- 原生句柄结果无效

如果向 `OverfloatSpec.number(...)` 传入不支持的 Python 类型，则会抛出 `TypeError`。

### 对象生命周期

Python API 的底层是显式管理的原生句柄。

可选清理方式：

- 对 `OverfloatSpec` 和 `OverfloatNumber` 调用 `close()`
- 使用 `with` 做确定性清理
- 仅把 `__del__` 当作兜底手段

推荐写法：

```python
from overfloat import OverfloatLibrary

lib = OverfloatLibrary()

with lib.create_spec_from_total_bits(32) as spec:
    with spec("1.5") as a:
        with spec("2.25") as b:
            print(a + b)
```

### 典型调用流程

Python 侧的常见调用流程如下：

1. 创建 `OverfloatLibrary`
2. 创建 `OverfloatSpec`
3. 把输入解析或转换成 `OverfloatNumber`
4. 执行算术或比较操作
5. 查看格式化结果、bit pattern 或异常标志
6. 在需要确定性清理时显式释放原生句柄

## 兼容性说明

- 公共 C ABI 应被视为兼容性边界。
- 原生库文件名和 wheel 内容属于打包契约的一部分。
- 任何改变数值舍入或格式化行为的修改，都应当同时更新文档并补测试。
