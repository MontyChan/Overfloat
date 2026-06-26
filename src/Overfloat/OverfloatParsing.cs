using System.Globalization;
using System.Numerics;

namespace Overfloat;

internal static class OverfloatParsing
{
    public static OverfloatNumber Parse(OverfloatSpecification specification, string text)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Input is empty.");
        }

        if (string.Equals(trimmed, "nan", StringComparison.OrdinalIgnoreCase))
        {
            return OverfloatNumber.CreateNaN(specification);
        }

        if (string.Equals(trimmed, "inf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "+inf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "infinity", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "+infinity", StringComparison.OrdinalIgnoreCase))
        {
            return OverfloatNumber.CreateInfinity(specification, false);
        }

        if (string.Equals(trimmed, "-inf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "-infinity", StringComparison.OrdinalIgnoreCase))
        {
            return OverfloatNumber.CreateInfinity(specification, true);
        }

        var rational = ParseRational(trimmed);
        if (rational.IsZero)
        {
            return OverfloatNumber.CreateZero(specification, trimmed.StartsWith("-", StringComparison.Ordinal));
        }

        return OverfloatMath.Quantize(specification, rational);
    }

    public static Rational ParseRational(string text)
    {
        var span = text.AsSpan().Trim();
        var negative = false;
        if (span.Length > 0 && (span[0] == '+' || span[0] == '-'))
        {
            negative = span[0] == '-';
            span = span[1..];
        }

        var exponent = 0;
        var eIndex = span.IndexOfAny('e', 'E');
        if (eIndex >= 0)
        {
            exponent = int.Parse(span[(eIndex + 1)..], CultureInfo.InvariantCulture);
            span = span[..eIndex];
        }

        var pointIndex = span.IndexOf('.');
        var digits = pointIndex >= 0
            ? string.Concat(span[..pointIndex].ToString(), span[(pointIndex + 1)..].ToString())
            : span.ToString();
        if (digits.Length == 0)
        {
            throw new FormatException("Input does not contain digits.");
        }

        var fractionDigits = pointIndex >= 0 ? span.Length - pointIndex - 1 : 0;
        var scale = fractionDigits - exponent;
        var numerator = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        if (negative)
        {
            numerator = BigInteger.Negate(numerator);
        }

        if (scale <= 0)
        {
            return new Rational(numerator * BigInteger.Pow(10, -scale), BigInteger.One);
        }

        return new Rational(numerator, BigInteger.Pow(10, scale));
    }
}
