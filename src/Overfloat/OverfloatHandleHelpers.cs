using System.Runtime.InteropServices;

namespace Overfloat;

internal static class OverfloatHandleHelpers
{
    public static nint Allocate<T>(T value) where T : class
    {
        return GCHandle.ToIntPtr(GCHandle.Alloc(value));
    }

    public static T? Get<T>(nint handle) where T : class
    {
        if (handle == nint.Zero)
        {
            return null;
        }

        var gcHandle = GCHandle.FromIntPtr(handle);
        return gcHandle.Target as T;
    }

    public static void Free(nint handle)
    {
        if (handle == nint.Zero)
        {
            return;
        }

        var gcHandle = GCHandle.FromIntPtr(handle);
        if (gcHandle.IsAllocated)
        {
            gcHandle.Free();
        }
    }
}
