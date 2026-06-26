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

        if (TryParseSpecialValue(specification, trimmed, out var specialValue))
        {
            return specialValue;
        }

        var rational = ParseRational(trimmed);
        if (rational.IsZero)
        {
            return OverfloatNumber.CreateZero(specification, trimmed.StartsWith("-", StringComparison.Ordinal));
        }

        return OverfloatMath.Quantize(specification, rational);
    }

    private static bool TryParseSpecialValue(OverfloatSpecification specification, string text, out OverfloatNumber number)
    {
        var span = text.AsSpan().Trim();
        var negative = false;
        if (span.Length > 0 && (span[0] == '+' || span[0] == '-'))
        {
            negative = span[0] == '-';
            span = span[1..];
        }

        if (span.Equals("inf".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.Equals("infinity".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            number = OverfloatNumber.CreateInfinity(specification, negative);
            return true;
        }

        var signaling = false;
        if (span.Equals("nan".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            number = OverfloatNumber.CreateNaN(specification, negative);
            return true;
        }

        if (span.StartsWith("nan(".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            number = OverfloatNumber.CreateNaN(specification, negative, signaling: false, payload: ParseNaNPayload(span, 3));
            return true;
        }

        if (span.Equals("snan".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            signaling = true;
            number = OverfloatNumber.CreateNaN(specification, negative, signaling);
            return true;
        }

        if (span.StartsWith("snan(".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            signaling = true;
            number = OverfloatNumber.CreateNaN(specification, negative, signaling, ParseNaNPayload(span, 4));
            return true;
        }

        number = null!;
        return false;
    }

    private static BigInteger ParseNaNPayload(ReadOnlySpan<char> span, int prefixLength)
    {
        if (span.Length <= prefixLength + 2 || span[prefixLength] != '(' || span[^1] != ')')
        {
            throw new FormatException("NaN payload must use the form NaN(<payload>) or sNaN(<payload>). ".TrimEnd());
        }

        var payloadText = span[(prefixLength + 1)..^1].Trim();
        if (payloadText.Length == 0)
        {
            throw new FormatException("NaN payload is empty.");
        }

        var payload = BigInteger.Parse(payloadText, CultureInfo.InvariantCulture);
        if (payload.Sign < 0)
        {
            throw new FormatException("NaN payload must be non-negative.");
        }

        return payload;
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
