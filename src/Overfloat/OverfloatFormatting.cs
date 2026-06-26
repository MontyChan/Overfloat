using System.Numerics;
using System.Text;

namespace Overfloat;

internal static class OverfloatFormatting
{
    public static string ToExactDecimalString(OverfloatNumber number)
    {
        return number.Classification switch
        {
            OverfloatClassification.NaN => "NaN",
            OverfloatClassification.Infinity => number.Negative ? "-Infinity" : "Infinity",
            OverfloatClassification.Zero => number.Negative ? "-0" : "0",
            _ => FormatFinite(number),
        };
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
