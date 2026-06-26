namespace Overfloat;

public sealed class OverfloatSpecification
{
    public OverfloatSpecification(int exponentBits, int mantissaBits, OverfloatRoundingMode roundingMode = OverfloatRoundingMode.ToNearestEven)
    {
        if (exponentBits < 2 || exponentBits > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentBits), "Exponent width must be between 2 and 30 bits.");
        }

        if (mantissaBits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mantissaBits), "Mantissa width must be at least 1 bit.");
        }

        ExponentBits = exponentBits;
        MantissaBits = mantissaBits;
        RoundingMode = roundingMode;
    }

    public int ExponentBits { get; }

    public int MantissaBits { get; }

    public OverfloatRoundingMode RoundingMode { get; }

    public int PrecisionBits => MantissaBits + 1;

    public int ExponentBias => (1 << (ExponentBits - 1)) - 1;

    public int ExponentFieldMask => (1 << ExponentBits) - 1;

    public int MaxFiniteExponentField => ExponentFieldMask - 1;

    public int MinNormalExponent => 1 - ExponentBias;

    public int MaxNormalExponent => ExponentBias;

    public OverfloatStatus Validate()
    {
        if (ExponentBits < 2 || ExponentBits > 30)
        {
            return OverfloatStatus.InvalidArgument;
        }

        if (MantissaBits < 1)
        {
            return OverfloatStatus.InvalidArgument;
        }

        return OverfloatStatus.Success;
    }
}
