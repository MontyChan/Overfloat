using System.Threading;

namespace Overfloat;

public static class OverfloatEnvironment
{
    private static readonly AsyncLocal<OverfloatExceptionFlags> CurrentFlags = new();

    public static OverfloatExceptionFlags ExceptionFlags => CurrentFlags.Value;

    public static void ClearExceptionFlags()
        => CurrentFlags.Value = OverfloatExceptionFlags.None;

    internal static void Raise(OverfloatExceptionFlags flags)
        => CurrentFlags.Value |= flags;
}
