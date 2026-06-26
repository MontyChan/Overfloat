using System.Runtime.InteropServices;
using System.Text;

namespace Overfloat;

internal static unsafe class Utf8Helpers
{
    public static string? ReadNullTerminated(byte* value)
    {
        if (value is null)
        {
            return null;
        }

        var length = 0;
        while (value[length] != 0)
        {
            length++;
        }

        return Encoding.UTF8.GetString(value, length);
    }

    public static int WriteNullTerminated(string value, byte* buffer, int bufferLength)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var required = byteCount + 1;
        if (bufferLength < required || buffer is null)
        {
            return required;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        Marshal.Copy(bytes, 0, (nint)buffer, bytes.Length);
        buffer[bytes.Length] = 0;
        return required;
    }
}
