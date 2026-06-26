using System.Numerics;

namespace Overfloat;

public static class OverfloatMath
{
    public static OverfloatNumber Add(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (left.Classification == OverfloatClassification.NaN || right.Classification == OverfloatClassification.NaN)
        {
            return OverfloatNumber.CreateNaN(left.Specification);
        }

        if (left.Classification == OverfloatClassification.Infinity || right.Classification == OverfloatClassification.Infinity)
        {
            if (left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Infinity && left.Negative != right.Negative)
            {
                return OverfloatNumber.CreateNaN(left.Specification);
            }

            return left.Classification == OverfloatClassification.Infinity ? left : right;
        }

        return Quantize(left.Specification, left.ToRational() + right.ToRational());
    }

    public static OverfloatNumber Subtract(OverfloatNumber left, OverfloatNumber right)
        => Add(left, right.Negate());

    public static OverfloatNumber Multiply(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (left.Classification == OverfloatClassification.NaN || right.Classification == OverfloatClassification.NaN)
        {
            return OverfloatNumber.CreateNaN(left.Specification);
        }

        if ((left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Zero) ||
            (right.Classification == OverfloatClassification.Infinity && left.Classification == OverfloatClassification.Zero))
        {
            return OverfloatNumber.CreateNaN(left.Specification);
        }

        if (left.Classification == OverfloatClassification.Infinity || right.Classification == OverfloatClassification.Infinity)
        {
            return OverfloatNumber.CreateInfinity(left.Specification, left.Negative ^ right.Negative);
        }

        return Quantize(left.Specification, left.ToRational() * right.ToRational());
    }

    public static OverfloatNumber Divide(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (left.Classification == OverfloatClassification.NaN || right.Classification == OverfloatClassification.NaN)
        {
            return OverfloatNumber.CreateNaN(left.Specification);
        }

        if ((left.Classification == OverfloatClassification.Zero && right.Classification == OverfloatClassification.Zero) ||
            (left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Infinity))
        {
            return OverfloatNumber.CreateNaN(left.Specification);
        }

        if (left.Classification == OverfloatClassification.Infinity)
        {
            return OverfloatNumber.CreateInfinity(left.Specification, left.Negative ^ right.Negative);
        }

        if (right.Classification == OverfloatClassification.Infinity)
        {
            return OverfloatNumber.CreateZero(left.Specification, left.Negative ^ right.Negative);
        }

        if (right.Classification == OverfloatClassification.Zero)
        {
            return left.Classification == OverfloatClassification.Zero
                ? OverfloatNumber.CreateNaN(left.Specification)
                : OverfloatNumber.CreateInfinity(left.Specification, left.Negative ^ right.Negative);
        }

        if (left.Classification == OverfloatClassification.Zero)
        {
            return OverfloatNumber.CreateZero(left.Specification, left.Negative ^ right.Negative);
        }

        return Quantize(left.Specification, left.ToRational() / right.ToRational());
    }

    internal static OverfloatNumber Quantize(OverfloatSpecification specification, Rational value)
    {
        ArgumentNullException.ThrowIfNull(specification);

        if (value.IsZero)
        {
            return OverfloatNumber.CreateZero(specification, false);
        }

        var negative = value.Sign < 0;
        var magnitude = value.Abs();
        var precisionBits = specification.PrecisionBits;
        var minNormalExponent = specification.MinNormalExponent;
        var maxNormalExponent = specification.MaxNormalExponent;
        var minNormalSignificand = BigIntegerExtensions.PowerOfTwo(precisionBits - 1);
        var normalExponent = FloorLog2(magnitude);

        if (normalExponent >= minNormalExponent)
        {
            var roundedSignificand = RoundScaled(magnitude, precisionBits - 1 - normalExponent, specification.RoundingMode, negative);
            if (roundedSignificand == BigIntegerExtensions.PowerOfTwo(precisionBits))
            {
                roundedSignificand >>= 1;
                normalExponent++;
            }

            if (normalExponent > maxNormalExponent)
            {
                return CreateOverflowValue(specification, negative);
            }

            return OverfloatNumber.CreateFinite(specification, negative, roundedSignificand, normalExponent - (precisionBits - 1));
        }

        var subnormalBinaryExponent = minNormalExponent - (precisionBits - 1);
        var subnormalSignificand = RoundScaled(magnitude, -subnormalBinaryExponent, specification.RoundingMode, negative);
        if (subnormalSignificand.IsZero)
        {
            return OverfloatNumber.CreateZero(specification, negative);
        }

        return OverfloatNumber.CreateFinite(specification, negative, subnormalSignificand, subnormalBinaryExponent);
    }

    private static OverfloatNumber CreateOverflowValue(OverfloatSpecification specification, bool negative)
    {
        return specification.RoundingMode switch
        {
            OverfloatRoundingMode.TowardZero => OverfloatNumber.CreateMaxFinite(specification, negative),
            OverfloatRoundingMode.TowardPositiveInfinity => negative ? OverfloatNumber.CreateMaxFinite(specification, true) : OverfloatNumber.CreateInfinity(specification, false),
            OverfloatRoundingMode.TowardNegativeInfinity => negative ? OverfloatNumber.CreateInfinity(specification, true) : OverfloatNumber.CreateMaxFinite(specification, false),
            _ => OverfloatNumber.CreateInfinity(specification, negative),
        };
    }

    private static void EnsureCompatible(OverfloatNumber left, OverfloatNumber right)
    {
        if (left.Specification.ExponentBits != right.Specification.ExponentBits ||
            left.Specification.MantissaBits != right.Specification.MantissaBits ||
            left.Specification.RoundingMode != right.Specification.RoundingMode)
        {
            throw new InvalidOperationException("Floating-point operands must share the same specification.");
        }
    }

    private static int FloorLog2(Rational value)
    {
        var numerator = BigInteger.Abs(value.Numerator);
        var denominator = value.Denominator;
        var exponent = checked((int)(numerator.GetBitLength() - denominator.GetBitLength()));

        var comparison = exponent >= 0
            ? numerator.CompareTo(denominator << exponent)
            : (numerator << -exponent).CompareTo(denominator);

        return comparison < 0 ? exponent - 1 : exponent;
    }

    private static BigInteger RoundScaled(Rational value, int powerOfTwoScale, OverfloatRoundingMode roundingMode, bool negative)
    {
        var numerator = BigInteger.Abs(value.Numerator);
        var denominator = value.Denominator;

        if (powerOfTwoScale >= 0)
        {
            numerator <<= powerOfTwoScale;
        }
        else
        {
            denominator <<= -powerOfTwoScale;
        }

        return RoundQuotient(numerator, denominator, roundingMode, negative);
    }

    private static BigInteger RoundQuotient(BigInteger numerator, BigInteger denominator, OverfloatRoundingMode roundingMode, bool negative)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        if (remainder.IsZero)
        {
            return quotient;
        }

        return roundingMode switch
        {
            OverfloatRoundingMode.ToNearestEven => RoundToNearestEven(quotient, remainder, denominator),
            OverfloatRoundingMode.TowardZero => quotient,
            OverfloatRoundingMode.AwayFromZero => quotient + BigInteger.One,
            OverfloatRoundingMode.TowardPositiveInfinity => negative ? quotient : quotient + BigInteger.One,
            OverfloatRoundingMode.TowardNegativeInfinity => negative ? quotient + BigInteger.One : quotient,
            _ => RoundToNearestEven(quotient, remainder, denominator),
        };
    }

    private static BigInteger RoundToNearestEven(BigInteger quotient, BigInteger remainder, BigInteger denominator)
    {
        var doubled = remainder << 1;
        if (doubled < denominator)
        {
            return quotient;
        }

        if (doubled > denominator)
        {
            return quotient + BigInteger.One;
        }

        return quotient.IsEven ? quotient : quotient + BigInteger.One;
    }
}
