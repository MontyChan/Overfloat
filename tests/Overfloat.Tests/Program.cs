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

        var onePointFive = Overfloat.OverfloatNumber.Parse(spec, "1.5");
        var twoPointTwoFive = Overfloat.OverfloatNumber.Parse(spec, "2.25");
        var sum = Overfloat.OverfloatMath.Add(onePointFive, twoPointTwoFive);
        if (sum.ToString() != "3.75")
        {
            return 3;
        }

        var product = Overfloat.OverfloatMath.Multiply(onePointFive, twoPointTwoFive);
        if (product.ToString() != "3.375")
        {
            return 4;
        }

        var quotient = Overfloat.OverfloatMath.Divide(Overfloat.OverfloatNumber.Parse(spec, "1"), Overfloat.OverfloatNumber.Parse(spec, "10"));
        if (quotient.Classification != Overfloat.OverfloatClassification.Normal && quotient.Classification != Overfloat.OverfloatClassification.Subnormal)
        {
            return 5;
        }

        var infinity = Overfloat.OverfloatNumber.Parse(spec, "inf");
        var negInfinity = Overfloat.OverfloatNumber.Parse(spec, "-inf");
        var nan = Overfloat.OverfloatMath.Add(infinity, negInfinity);
        return nan.Classification == Overfloat.OverfloatClassification.NaN ? 0 : 6;
    }
}
