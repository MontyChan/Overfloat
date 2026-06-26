using System.Numerics;

namespace Overfloat.Tests;

public static class Program
{
    public static int Main()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        if (spec.ExponentBits != 8 || spec.MantissaBits != 23)
        {
            return 1;
        }

        if (Overfloat.OverfloatValidation.ValidateSpecification(8, 23) != Overfloat.OverfloatStatus.Success)
        {
            return 2;
        }

        if (!ValidateStandardSpec(16, 5, 10, new BigInteger(15)))
        {
            return 3;
        }

        if (!ValidateStandardSpec(32, 8, 23, new BigInteger(127)))
        {
            return 4;
        }

        if (!ValidateStandardSpec(64, 11, 52, new BigInteger(1023)))
        {
            return 5;
        }

        if (!ValidateStandardSpec(128, 15, 112, new BigInteger(16383)))
        {
            return 6;
        }

        if (!ValidateStandardSpec(256, 19, 236, new BigInteger(262143)))
        {
            return 7;
        }

        if (!ValidateStandardSpec(16384, 43, 16340, BigInteger.Parse("4398046511103")))
        {
            return 8;
        }

        if (!ThrowsArgumentException(() => Overfloat.OverfloatSpecification.FromTotalBits(24)))
        {
            return 9;
        }

        if (!ThrowsArgumentException(() => Overfloat.OverfloatSpecification.FromTotalBits(100)))
        {
            return 10;
        }

        if (!ThrowsArgumentException(() => Overfloat.OverfloatSpecification.FromTotalBits(130)))
        {
            return 11;
        }

        var onePointFive = Overfloat.OverfloatNumber.Parse(spec, "1.5");
        var twoPointTwoFive = Overfloat.OverfloatNumber.Parse(spec, "2.25");
        var sum = Overfloat.OverfloatMath.Add(onePointFive, twoPointTwoFive);
        if (sum.ToString() != "3.75")
        {
            return 12;
        }

        var product = Overfloat.OverfloatMath.Multiply(onePointFive, twoPointTwoFive);
        if (product.ToString() != "3.375")
        {
            return 13;
        }

        var quotient = Overfloat.OverfloatMath.Divide(Overfloat.OverfloatNumber.Parse(spec, "1"), Overfloat.OverfloatNumber.Parse(spec, "10"));
        if (quotient.Classification != Overfloat.OverfloatClassification.Normal && quotient.Classification != Overfloat.OverfloatClassification.Subnormal)
        {
            return 14;
        }

        var infinity = Overfloat.OverfloatNumber.Parse(spec, "inf");
        var negInfinity = Overfloat.OverfloatNumber.Parse(spec, "-inf");
        var nan = Overfloat.OverfloatMath.Add(infinity, negInfinity);
        if (nan.Classification != Overfloat.OverfloatClassification.NaN)
        {
            return 15;
        }

        var towardNegative = new Overfloat.OverfloatSpecification(8, 23, Overfloat.OverfloatRoundingMode.TowardNegativeInfinity);
        var negZero = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(towardNegative, "1"),
            Overfloat.OverfloatNumber.Parse(towardNegative, "-1"));
        if (negZero.Classification != Overfloat.OverfloatClassification.Zero || !negZero.Negative || negZero.ToString() != "-0")
        {
            return 16;
        }

        var nearest = new Overfloat.OverfloatSpecification(8, 23, Overfloat.OverfloatRoundingMode.ToNearestEven);
        var posZero = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(nearest, "1"),
            Overfloat.OverfloatNumber.Parse(nearest, "-1"));
        return posZero.Classification == Overfloat.OverfloatClassification.Zero && !posZero.Negative && posZero.ToString() == "0" ? 0 : 17;
    }

    private static bool ValidateStandardSpec(int totalBits, int expectedExponentBits, int expectedMantissaBits, BigInteger expectedBias)
    {
        var spec = Overfloat.OverfloatSpecification.FromTotalBits(totalBits);
        return spec.TotalBits == totalBits &&
               spec.ExponentBits == expectedExponentBits &&
               spec.MantissaBits == expectedMantissaBits &&
               spec.ExponentBias == expectedBias;
    }

    private static bool ThrowsArgumentException(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}
