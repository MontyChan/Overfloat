using System.Numerics;

namespace Overfloat;

internal static class BigIntegerExtensions
{
    public static int GetBitLength(this BigInteger value)
    {
        if (value.Sign == 0)
        {
            return 0;
        }

        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var bits = (bytes.Length - 1) * 8;
        var msb = bytes[0];
        while (msb > 0)
        {
            bits++;
            msb >>= 1;
        }

        return bits;
    }

    public static BigInteger PowerOfTwo(int exponent)
        => exponent < 0 ? throw new ArgumentOutOfRangeException(nameof(exponent)) : BigInteger.One << exponent;

    public static BigInteger PowerOfFive(int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent));
        }

        var result = BigInteger.One;
        for (var i = 0; i < exponent; i++)
        {
            result *= 5;
        }

        return result;
    }
}
