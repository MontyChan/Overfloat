using System.Globalization;
using System.Numerics;

namespace Overfloat;

public static class OverfloatBitConverter
{
    public static OverfloatNumber FromHexString(OverfloatSpecification specification, string hex)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(hex);

        var trimmed = hex.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
        }

        if (trimmed.Length == 0)
        {
            throw new FormatException("Bit pattern is empty.");
        }

        var maxHexDigits = (specification.TotalBits + 3) / 4;
        if (trimmed.Length > maxHexDigits)
        {
            throw new FormatException("Bit pattern exceeds the specification width.");
        }

        var bits = BigInteger.Parse($"0{trimmed}", NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        var totalBits = specification.TotalBits;
        var limit = BigInteger.One << totalBits;
        if (bits < BigInteger.Zero || bits >= limit)
        {
            throw new FormatException("Bit pattern exceeds the specification width.");
        }

        return FromBitPattern(specification, bits);
    }

    public static string ToHexString(OverfloatNumber number)
    {
        ArgumentNullException.ThrowIfNull(number);

        var hexDigits = (number.Specification.TotalBits + 3) / 4;
        var bits = EncodeToBitPattern(number);
        var hex = bits.ToString("X", CultureInfo.InvariantCulture);
        if (hex.Length > hexDigits)
        {
            hex = hex[^hexDigits..];
        }

        return hex.PadLeft(hexDigits, '0');
    }

    internal static BigInteger EncodeToBitPattern(OverfloatNumber number)
    {
        ArgumentNullException.ThrowIfNull(number);

        var specification = number.Specification;
        var exponentField = BigInteger.Zero;
        var fractionField = BigInteger.Zero;
        var fractionMask = BigIntegerExtensions.PowerOfTwo(specification.MantissaBits) - BigInteger.One;

        switch (number.Classification)
        {
            case OverfloatClassification.Zero:
                break;

            case OverfloatClassification.Subnormal:
                fractionField = number.SignificandBits;
                break;

            case OverfloatClassification.Normal:
                exponentField = specification.ExponentBias + number.BinaryExponent + specification.MantissaBits;
                fractionField = number.SignificandBits - BigIntegerExtensions.PowerOfTwo(specification.MantissaBits);
                break;

            case OverfloatClassification.Infinity:
                exponentField = specification.ExponentFieldMask;
                break;

            case OverfloatClassification.NaN:
                exponentField = specification.ExponentFieldMask;
                var quietBit = BigIntegerExtensions.PowerOfTwo(specification.MantissaBits - 1);
                var payloadMask = quietBit - BigInteger.One;
                var payload = number.NaNPayload & payloadMask;
                if (number.IsSignalingNaN && payload.IsZero)
                {
                    if (!payloadMask.IsZero)
                    {
                        payload = BigInteger.One;
                    }
                    else
                    {
                        return EncodeToBitPattern(OverfloatNumber.CreateNaN(specification, number.Negative, signaling: false, payload: BigInteger.Zero));
                    }
                }

                fractionField = payload;
                if (!number.IsSignalingNaN)
                {
                    fractionField |= quietBit;
                }
                break;

            default:
                throw new NotSupportedException("Unknown floating-point classification.");
        }

        if (fractionField < BigInteger.Zero || fractionField > fractionMask)
        {
            throw new InvalidOperationException("Fraction field does not fit the specification.");
        }

        if (exponentField < BigInteger.Zero || exponentField > specification.ExponentFieldMask)
        {
            throw new InvalidOperationException("Exponent field does not fit the specification.");
        }

        var signField = number.Negative ? BigInteger.One << (specification.TotalBits - 1) : BigInteger.Zero;
        return signField | (exponentField << specification.MantissaBits) | fractionField;
    }

    private static OverfloatNumber FromBitPattern(OverfloatSpecification specification, BigInteger bits)
    {
        var signBit = BigInteger.One << (specification.TotalBits - 1);
        var negative = (bits & signBit) != BigInteger.Zero;
        var fractionMask = BigIntegerExtensions.PowerOfTwo(specification.MantissaBits) - BigInteger.One;
        var exponentField = (bits >> specification.MantissaBits) & specification.ExponentFieldMask;
        var fractionField = bits & fractionMask;

        if (exponentField.IsZero)
        {
            if (fractionField.IsZero)
            {
                return OverfloatNumber.CreateZero(specification, negative);
            }

            var subnormalBinaryExponent = specification.MinNormalExponent - specification.MantissaBits;
            return OverfloatNumber.CreateFinite(specification, negative, fractionField, checked((int)subnormalBinaryExponent));
        }

        if (exponentField == specification.ExponentFieldMask)
        {
            if (fractionField.IsZero)
            {
                return OverfloatNumber.CreateInfinity(specification, negative);
            }

            var quietBit = BigIntegerExtensions.PowerOfTwo(specification.MantissaBits - 1);
            var payload = fractionField & (quietBit - BigInteger.One);
            var signaling = (fractionField & quietBit).IsZero;
            return OverfloatNumber.CreateNaN(specification, negative, signaling, payload);
        }

        var unbiasedExponent = exponentField - specification.ExponentBias;
        var binaryExponent = unbiasedExponent - specification.MantissaBits;
        var significand = BigIntegerExtensions.PowerOfTwo(specification.MantissaBits) | fractionField;
        return OverfloatNumber.CreateFinite(specification, negative, significand, checked((int)binaryExponent));
    }
}
