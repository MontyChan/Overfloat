using System.Globalization;
using System.Numerics;
using System.Text;

namespace Overfloat;

internal static class OverfloatFormatting
{
    public static string ToExactDecimalString(OverfloatNumber number)
    {
        return number.Classification switch
        {
            OverfloatClassification.NaN => FormatNaN(number),
            OverfloatClassification.Infinity => number.Negative ? "-Infinity" : "Infinity",
            OverfloatClassification.Zero => number.Negative ? "-0" : "0",
            _ => FormatFinite(number),
        };
    }

    private static string FormatNaN(OverfloatNumber number)
    {
        var sign = number.Negative ? "-" : string.Empty;
        var kind = number.IsSignalingNaN ? "sNaN" : "NaN";
        return number.NaNPayload.IsZero
            ? sign + kind
            : string.Format(CultureInfo.InvariantCulture, "{0}{1}({2})", sign, kind, number.NaNPayload);
    }

    private static string FormatFinite(OverfloatNumber number)
    {
        var significand = number.SignificandBits;
        var exponent = number.BinaryExponent;
        string unsigned;

        if (exponent >= 0)
        {
            unsigned = (significand << exponent).ToString();
        }
        else
        {
            var scale = -exponent;
            var scaled = significand * BigIntegerExtensions.PowerOfFive(scale);
            var digits = scaled.ToString();
            if (scale >= digits.Length)
            {
                unsigned = "0." + new string('0', scale - digits.Length) + digits;
            }
            else
            {
                unsigned = digits[..(digits.Length - scale)] + "." + digits[(digits.Length - scale)..];
            }

            unsigned = unsigned.TrimEnd('0').TrimEnd('.');
            if (unsigned.Length == 0)
            {
                unsigned = "0";
            }
        }

        return number.Negative ? "-" + unsigned : unsigned;
    }
}
