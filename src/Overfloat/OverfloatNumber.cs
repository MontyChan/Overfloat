using System.Globalization;
using System.Numerics;

namespace Overfloat;

public sealed class OverfloatNumber
{
    private OverfloatNumber(
        OverfloatSpecification specification,
        bool negative,
        int binaryExponent,
        BigInteger significandBits,
        OverfloatClassification classification,
        bool isSignalingNaN,
        BigInteger nanPayload)
    {
        Specification = specification ?? throw new ArgumentNullException(nameof(specification));
        Negative = negative;
        BinaryExponent = binaryExponent;
        SignificandBits = BigInteger.Abs(significandBits);
        Classification = classification;

        if (classification == OverfloatClassification.NaN)
        {
            var quietPayloadBits = specification.MantissaBits - 1;
            var payloadMask = quietPayloadBits > 0
                ? BigIntegerExtensions.PowerOfTwo(quietPayloadBits) - BigInteger.One
                : BigInteger.Zero;

            NaNPayload = BigInteger.Abs(nanPayload) & payloadMask;
            IsSignalingNaN = isSignalingNaN && quietPayloadBits > 0;
            if (IsSignalingNaN && NaNPayload.IsZero)
            {
                NaNPayload = BigInteger.One;
            }
        }
        else
        {
            IsSignalingNaN = false;
            NaNPayload = BigInteger.Zero;
        }
    }

    public OverfloatSpecification Specification { get; }

    public bool Negative { get; }

    public int BinaryExponent { get; }

    public BigInteger SignificandBits { get; }

    public OverfloatClassification Classification { get; }

    public bool IsSignalingNaN { get; }

    public BigInteger NaNPayload { get; }

    public bool IsSpecial => Classification is OverfloatClassification.NaN or OverfloatClassification.Infinity;

    public static OverfloatNumber Parse(OverfloatSpecification specification, string text)
        => OverfloatParsing.Parse(specification, text);

    public static OverfloatNumber CreateFinite(OverfloatSpecification specification, bool negative, BigInteger significandBits, int binaryExponent)
    {
        if (significandBits.IsZero)
        {
            return CreateZero(specification, negative);
        }

        var minNormalSignificand = BigIntegerExtensions.PowerOfTwo(specification.PrecisionBits - 1);
        var classification = significandBits < minNormalSignificand
            ? OverfloatClassification.Subnormal
            : OverfloatClassification.Normal;

        return new OverfloatNumber(specification, negative, binaryExponent, significandBits, classification, false, BigInteger.Zero);
    }

    public static OverfloatNumber CreateZero(OverfloatSpecification specification, bool negative)
        => new(specification, negative, 0, BigInteger.Zero, OverfloatClassification.Zero, false, BigInteger.Zero);

    public static OverfloatNumber CreateInfinity(OverfloatSpecification specification, bool negative)
        => new(specification, negative, 0, BigInteger.Zero, OverfloatClassification.Infinity, false, BigInteger.Zero);

    public static OverfloatNumber CreateNaN(OverfloatSpecification specification, bool negative = false, bool signaling = false, BigInteger payload = default)
        => new(specification, negative, 0, BigInteger.Zero, OverfloatClassification.NaN, signaling, payload);

    public static OverfloatNumber CreateMaxFinite(OverfloatSpecification specification, bool negative)
    {
        var significand = BigIntegerExtensions.PowerOfTwo(specification.PrecisionBits) - BigInteger.One;
        var exponent = checked((int)(specification.MaxNormalExponent - (specification.PrecisionBits - 1)));
        return CreateFinite(specification, negative, significand, exponent);
    }

    public OverfloatNumber Negate()
        => new(Specification, !Negative, BinaryExponent, SignificandBits, Classification, IsSignalingNaN, NaNPayload);

    public OverfloatNumber QuietNaN()
    {
        if (Classification != OverfloatClassification.NaN || !IsSignalingNaN)
        {
            return this;
        }

        return CreateNaN(Specification, Negative, signaling: false, NaNPayload);
    }

    public string ToBitPatternHex()
        => OverfloatBitConverter.ToHexString(this);

    internal Rational ToRational()
    {
        if (Classification == OverfloatClassification.Zero)
        {
            return new Rational(BigInteger.Zero, BigInteger.One);
        }

        if (IsSpecial)
        {
            throw new InvalidOperationException("Special floating-point values do not have a finite rational form.");
        }

        var numerator = SignificandBits;
        var denominator = BigInteger.One;
        if (BinaryExponent >= 0)
        {
            numerator <<= BinaryExponent;
        }
        else
        {
            denominator = BigIntegerExtensions.PowerOfTwo(-BinaryExponent);
        }

        if (Negative)
        {
            numerator = BigInteger.Negate(numerator);
        }

        return new Rational(numerator, denominator);
    }


    public override string ToString()
        => OverfloatFormatting.ToExactDecimalString(this);

    public string ToDiagnosticString()
    {
        if (Classification != OverfloatClassification.NaN)
        {
            return ToString();
        }

        var sign = Negative ? "-" : string.Empty;
        var kind = IsSignalingNaN ? "sNaN" : "NaN";
        return NaNPayload.IsZero
            ? sign + kind
            : string.Format(CultureInfo.InvariantCulture, "{0}{1}({2})", sign, kind, NaNPayload);
    }
}
