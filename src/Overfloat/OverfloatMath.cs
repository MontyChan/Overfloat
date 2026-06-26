using System.Numerics;

namespace Overfloat;

public static class OverfloatMath
{
    public static OverfloatNumber Add(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (TryPropagateNaN(left, right, out var propagatedNaN))
        {
            return propagatedNaN;
        }

        if (left.Classification == OverfloatClassification.Infinity || right.Classification == OverfloatClassification.Infinity)
        {
            if (left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Infinity && left.Negative != right.Negative)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
                return CreateQuietNaN(left, right);
            }

            return left.Classification == OverfloatClassification.Infinity ? left : right;
        }

        var sum = left.ToRational() + right.ToRational();
        if (sum.IsZero)
        {
            return CreateExactZeroValue(left.Specification);
        }

        return Quantize(left.Specification, sum);
    }

    public static OverfloatNumber Subtract(OverfloatNumber left, OverfloatNumber right)
        => Add(left, right.Negate());

    public static OverfloatNumber Multiply(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (TryPropagateNaN(left, right, out var propagatedNaN))
        {
            return propagatedNaN;
        }

        if ((left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Zero) ||
            (right.Classification == OverfloatClassification.Infinity && left.Classification == OverfloatClassification.Zero))
        {
            OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
            return CreateQuietNaN(left, right);
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

        if (TryPropagateNaN(left, right, out var propagatedNaN))
        {
            return propagatedNaN;
        }

        if ((left.Classification == OverfloatClassification.Zero && right.Classification == OverfloatClassification.Zero) ||
            (left.Classification == OverfloatClassification.Infinity && right.Classification == OverfloatClassification.Infinity))
        {
            OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
            return CreateQuietNaN(left, right);
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
            if (left.Classification == OverfloatClassification.Zero)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
                return CreateQuietNaN(left, right);
            }

            OverfloatEnvironment.Raise(OverfloatExceptionFlags.DivideByZero);
            return OverfloatNumber.CreateInfinity(left.Specification, left.Negative ^ right.Negative);
        }

        if (left.Classification == OverfloatClassification.Zero)
        {
            return OverfloatNumber.CreateZero(left.Specification, left.Negative ^ right.Negative);
        }

        return Quantize(left.Specification, left.ToRational() / right.ToRational());
    }

    public static int Compare(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        if (left.Classification == OverfloatClassification.NaN || right.Classification == OverfloatClassification.NaN)
        {
            OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
            throw new InvalidOperationException("Ordered comparison is undefined when either operand is NaN.");
        }

        if (left.Classification == OverfloatClassification.Zero && right.Classification == OverfloatClassification.Zero)
        {
            return 0;
        }

        if (left.Classification == OverfloatClassification.Infinity || right.Classification == OverfloatClassification.Infinity)
        {
            if (left.Classification == right.Classification)
            {
                if (left.Negative == right.Negative)
                {
                    return 0;
                }

                return left.Negative ? -1 : 1;
            }

            if (left.Classification == OverfloatClassification.Infinity)
            {
                return left.Negative ? -1 : 1;
            }

            return right.Negative ? 1 : -1;
        }

        if (left.Negative != right.Negative)
        {
            return left.Negative ? -1 : 1;
        }

        return left.ToRational().CompareTo(right.ToRational());
    }

    public static int CompareTotal(OverfloatNumber left, OverfloatNumber right)
    {
        EnsureCompatible(left, right);

        var leftBits = OverfloatBitConverter.EncodeToBitPattern(left);
        var rightBits = OverfloatBitConverter.EncodeToBitPattern(right);
        if (leftBits == rightBits)
        {
            return 0;
        }

        if (left.Negative != right.Negative)
        {
            return left.Negative ? -1 : 1;
        }

        return left.Negative
            ? rightBits.CompareTo(leftBits)
            : leftBits.CompareTo(rightBits);
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
        var normalExponent = FloorLog2(magnitude);

        if (new BigInteger(normalExponent) >= minNormalExponent)
        {
            var roundedSignificand = RoundScaled(magnitude, precisionBits - 1 - normalExponent, specification.RoundingMode, negative, out var inexactNormal);
            if (roundedSignificand == BigIntegerExtensions.PowerOfTwo(precisionBits))
            {
                roundedSignificand >>= 1;
                normalExponent++;
            }

            if (new BigInteger(normalExponent) > maxNormalExponent)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Overflow | OverfloatExceptionFlags.Inexact);
                return CreateOverflowValue(specification, negative);
            }

            if (inexactNormal)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Inexact);
            }

            return OverfloatNumber.CreateFinite(specification, negative, roundedSignificand, normalExponent - (precisionBits - 1));
        }

        var subnormalBinaryExponent = minNormalExponent - (precisionBits - 1);
        if (subnormalBinaryExponent < int.MinValue)
        {
            OverfloatEnvironment.Raise(OverfloatExceptionFlags.Underflow | OverfloatExceptionFlags.Inexact);
            return OverfloatNumber.CreateZero(specification, negative);
        }

        var subnormalBinaryExponentInt = (int)subnormalBinaryExponent;
        var subnormalSignificand = RoundScaled(magnitude, -subnormalBinaryExponentInt, specification.RoundingMode, negative, out var inexactSubnormal);
        if (subnormalSignificand.IsZero)
        {
            if (inexactSubnormal)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Underflow | OverfloatExceptionFlags.Inexact);
            }

            return OverfloatNumber.CreateZero(specification, negative);
        }

        if (inexactSubnormal)
        {
            OverfloatEnvironment.Raise(OverfloatExceptionFlags.Underflow | OverfloatExceptionFlags.Inexact);
        }

        return OverfloatNumber.CreateFinite(specification, negative, subnormalSignificand, subnormalBinaryExponentInt);
    }

    private static OverfloatNumber CreateExactZeroValue(OverfloatSpecification specification)
    {
        var negative = specification.RoundingMode == OverfloatRoundingMode.TowardNegativeInfinity;
        return OverfloatNumber.CreateZero(specification, negative);
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

    private static bool TryPropagateNaN(OverfloatNumber left, OverfloatNumber right, out OverfloatNumber result)
    {
        if (left.Classification == OverfloatClassification.NaN)
        {
            if (left.IsSignalingNaN || right.IsSignalingNaN)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
            }

            result = left.QuietNaN();
            return true;
        }

        if (right.Classification == OverfloatClassification.NaN)
        {
            if (right.IsSignalingNaN)
            {
                OverfloatEnvironment.Raise(OverfloatExceptionFlags.Invalid);
            }

            result = right.QuietNaN();
            return true;
        }

        result = null!;
        return false;
    }

    private static OverfloatNumber CreateQuietNaN(OverfloatNumber left, OverfloatNumber right)
    {
        if (left.Classification == OverfloatClassification.NaN)
        {
            return left.QuietNaN();
        }

        if (right.Classification == OverfloatClassification.NaN)
        {
            return right.QuietNaN();
        }

        return OverfloatNumber.CreateNaN(left.Specification);
    }

    private static BigInteger RoundScaled(Rational value, int powerOfTwoScale, OverfloatRoundingMode roundingMode, bool negative, out bool inexact)
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

        return RoundQuotient(numerator, denominator, roundingMode, negative, out inexact);
    }

    private static BigInteger RoundQuotient(BigInteger numerator, BigInteger denominator, OverfloatRoundingMode roundingMode, bool negative, out bool inexact)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        inexact = !remainder.IsZero;
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
