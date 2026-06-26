namespace Overfloat;

[Flags]
public enum OverfloatExceptionFlags
{
    None = 0,
    Invalid = 1 << 0,
    DivideByZero = 1 << 1,
    Overflow = 1 << 2,
    Underflow = 1 << 3,
    Inexact = 1 << 4,
}
