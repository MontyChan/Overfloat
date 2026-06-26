using System.Numerics;

namespace Overfloat;

public sealed class OverfloatSpecification
{
    public OverfloatSpecification(int exponentBits, int mantissaBits, OverfloatRoundingMode roundingMode = OverfloatRoundingMode.ToNearestEven)
    {
        if (exponentBits < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentBits), "Exponent width must be at least 2 bits.");
        }

        if (mantissaBits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mantissaBits), "Mantissa width must be at least 1 bit.");
        }

        ExponentBits = exponentBits;
        MantissaBits = mantissaBits;
        RoundingMode = roundingMode;
        ExponentBias = (BigInteger.One << (ExponentBits - 1)) - BigInteger.One;
        ExponentFieldMask = (BigInteger.One << ExponentBits) - BigInteger.One;
        MaxFiniteExponentField = ExponentFieldMask - BigInteger.One;
        MinNormalExponent = BigInteger.One - ExponentBias;
        MaxNormalExponent = ExponentBias;
    }

    public int ExponentBits { get; }

    public int MantissaBits { get; }

    public OverfloatRoundingMode RoundingMode { get; }

    public BigInteger ExponentBias { get; }

    public BigInteger ExponentFieldMask { get; }

    public BigInteger MaxFiniteExponentField { get; }

    public BigInteger MinNormalExponent { get; }

    public BigInteger MaxNormalExponent { get; }

    public int PrecisionBits => MantissaBits + 1;

    public int TotalBits => ExponentBits + MantissaBits + 1;

    public OverfloatStatus Validate()
    {
        if (ExponentBits < 2)
        {
            return OverfloatStatus.InvalidArgument;
        }

        if (MantissaBits < 1)
        {
            return OverfloatStatus.InvalidArgument;
        }

        return OverfloatStatus.Success;
    }

    public static OverfloatSpecification FromTotalBits(int totalBits, OverfloatRoundingMode roundingMode = OverfloatRoundingMode.ToNearestEven)
    {
        var (exponentBits, mantissaBits) = ResolveStandardBitWidths(totalBits);
        return new OverfloatSpecification(exponentBits, mantissaBits, roundingMode);
    }

    private static (int ExponentBits, int MantissaBits) ResolveStandardBitWidths(int totalBits)
    {
        return totalBits switch
        {
            16 => (5, 10),
            32 => (8, 23),
            64 => (11, 52),
            128 => (15, 112),
            < 16 => throw new ArgumentException("This total bit width has no standard definition. Use create_spec(exponent_bits, mantissa_bits) to specify widths manually.", nameof(totalBits)),
            _ when totalBits > 128 && totalBits % 32 != 0 => throw new ArgumentException("Total bit width must be a multiple of 32 for IEEE 754-2008 binary interchange format extensions greater than 128 bits.", nameof(totalBits)),
            _ when totalBits > 128 => ResolveExtendedBitWidths(totalBits),
            _ => throw new ArgumentException("This total bit width has no standard definition. Use create_spec(exponent_bits, mantissa_bits) to specify widths manually.", nameof(totalBits)),
        };
    }

    private static (int ExponentBits, int MantissaBits) ResolveExtendedBitWidths(int totalBits)
    {
        var exponentBits = checked((int)(Math.Round(4.0 * Math.Log2(totalBits), MidpointRounding.ToEven) - 13.0));
        var mantissaBits = totalBits - exponentBits - 1;

        if (exponentBits < 2 || mantissaBits < 1)
        {
            throw new ArgumentException("Unable to derive IEEE 754-2008 binary interchange format widths for the specified total bit width.", nameof(totalBits));
        }

        return (exponentBits, mantissaBits);
    }
}
