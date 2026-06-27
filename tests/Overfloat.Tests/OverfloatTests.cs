using System.Numerics;
using Xunit;

namespace Overfloat.Tests;

public sealed class OverfloatTests
{
    [Fact]
    public void Specification_ExposesConfiguredWidths()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);

        Assert.Equal(8, spec.ExponentBits);
        Assert.Equal(23, spec.MantissaBits);
    }

    [Fact]
    public void Validation_AcceptsStandardSinglePrecision()
    {
        Assert.Equal(
            Overfloat.OverfloatStatus.Success,
            Overfloat.OverfloatValidation.ValidateSpecification(8, 23));
    }

    [Theory]
    [InlineData(16, 5, 10, "15")]
    [InlineData(32, 8, 23, "127")]
    [InlineData(64, 11, 52, "1023")]
    [InlineData(128, 15, 112, "16383")]
    [InlineData(256, 19, 236, "262143")]
    [InlineData(16384, 43, 16340, "4398046511103")]
    public void StandardFormats_AreResolvedCorrectly(int totalBits, int expectedExponentBits, int expectedMantissaBits, string expectedBias)
    {
        var spec = Overfloat.OverfloatSpecification.FromTotalBits(totalBits);

        Assert.Equal(totalBits, spec.TotalBits);
        Assert.Equal(expectedExponentBits, spec.ExponentBits);
        Assert.Equal(expectedMantissaBits, spec.MantissaBits);
        Assert.Equal(BigInteger.Parse(expectedBias), spec.ExponentBias);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(100)]
    [InlineData(130)]
    public void UnsupportedStandardFormats_Throw(int totalBits)
    {
        Assert.Throws<ArgumentException>(() => Overfloat.OverfloatSpecification.FromTotalBits(totalBits));
    }

    [Fact]
    public void Add_FormatsExpectedResult()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var sum = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(spec, "1.5"),
            Overfloat.OverfloatNumber.Parse(spec, "2.25"));

        Assert.Equal("3.75", sum.ToString());
    }

    [Fact]
    public void Multiply_FormatsExpectedResult()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var product = Overfloat.OverfloatMath.Multiply(
            Overfloat.OverfloatNumber.Parse(spec, "1.5"),
            Overfloat.OverfloatNumber.Parse(spec, "2.25"));

        Assert.Equal("3.375", product.ToString());
    }

    [Fact]
    public void Divide_ProducesFiniteValueForOneTenth()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var quotient = Overfloat.OverfloatMath.Divide(
            Overfloat.OverfloatNumber.Parse(spec, "1"),
            Overfloat.OverfloatNumber.Parse(spec, "10"));

        Assert.Contains(
            quotient.Classification,
            new[] { Overfloat.OverfloatClassification.Normal, Overfloat.OverfloatClassification.Subnormal });
    }

    [Fact]
    public void AddingOppositeInfinities_ProducesNaN()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var nan = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(spec, "inf"),
            Overfloat.OverfloatNumber.Parse(spec, "-inf"));

        Assert.Equal(Overfloat.OverfloatClassification.NaN, nan.Classification);
    }

    [Fact]
    public void ExactZeroSign_FollowsTowardNegativeRoundingMode()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23, Overfloat.OverfloatRoundingMode.TowardNegativeInfinity);
        var zero = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(spec, "1"),
            Overfloat.OverfloatNumber.Parse(spec, "-1"));

        Assert.Equal(Overfloat.OverfloatClassification.Zero, zero.Classification);
        Assert.True(zero.Negative);
        Assert.Equal("-0", zero.ToString());
    }

    [Fact]
    public void ExactZeroSign_FollowsNearestEvenRoundingMode()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23, Overfloat.OverfloatRoundingMode.ToNearestEven);
        var zero = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(spec, "1"),
            Overfloat.OverfloatNumber.Parse(spec, "-1"));

        Assert.Equal(Overfloat.OverfloatClassification.Zero, zero.Classification);
        Assert.False(zero.Negative);
        Assert.Equal("0", zero.ToString());
    }

    [Fact]
    public void SignalingNaN_IsQuietedAndRaisesInvalid()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        Overfloat.OverfloatEnvironment.ClearExceptionFlags();

        var quietNaN = Overfloat.OverfloatMath.Add(
            Overfloat.OverfloatNumber.Parse(spec, "sNaN(7)"),
            Overfloat.OverfloatNumber.Parse(spec, "1.5"));

        Assert.Equal(Overfloat.OverfloatClassification.NaN, quietNaN.Classification);
        Assert.False(quietNaN.IsSignalingNaN);
        Assert.Equal(new BigInteger(7), quietNaN.NaNPayload);
        Assert.NotEqual(
            Overfloat.OverfloatExceptionFlags.None,
            Overfloat.OverfloatEnvironment.ExceptionFlags & Overfloat.OverfloatExceptionFlags.Invalid);
    }

    [Fact]
    public void DivisionByZero_RaisesFlagAndReturnsInfinity()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        Overfloat.OverfloatEnvironment.ClearExceptionFlags();

        var result = Overfloat.OverfloatMath.Divide(
            Overfloat.OverfloatNumber.Parse(spec, "1"),
            Overfloat.OverfloatNumber.Parse(spec, "0"));

        Assert.Equal(Overfloat.OverfloatClassification.Infinity, result.Classification);
        Assert.NotEqual(
            Overfloat.OverfloatExceptionFlags.None,
            Overfloat.OverfloatEnvironment.ExceptionFlags & Overfloat.OverfloatExceptionFlags.DivideByZero);
    }

    [Fact]
    public void InexactDivision_RaisesInexactFlag()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        Overfloat.OverfloatEnvironment.ClearExceptionFlags();

        var result = Overfloat.OverfloatMath.Divide(
            Overfloat.OverfloatNumber.Parse(spec, "1"),
            Overfloat.OverfloatNumber.Parse(spec, "10"));

        Assert.Contains(
            result.Classification,
            new[] { Overfloat.OverfloatClassification.Normal, Overfloat.OverfloatClassification.Subnormal });
        Assert.NotEqual(
            Overfloat.OverfloatExceptionFlags.None,
            Overfloat.OverfloatEnvironment.ExceptionFlags & Overfloat.OverfloatExceptionFlags.Inexact);
    }

    [Fact]
    public void BitPattern_RoundTripsForFiniteNumber()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var value = Overfloat.OverfloatNumber.Parse(spec, "1.5");
        var bitPattern = value.ToBitPatternHex();
        var roundTrip = Overfloat.OverfloatBitConverter.FromHexString(spec, bitPattern);

        Assert.Equal(bitPattern, roundTrip.ToBitPatternHex());
        Assert.Equal(value.ToString(), roundTrip.ToString());
    }

    [Fact]
    public void BitPattern_RoundTripsForNegativeZero()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var bitPattern = Overfloat.OverfloatNumber.CreateZero(spec, true).ToBitPatternHex();
        var roundTrip = Overfloat.OverfloatBitConverter.FromHexString(spec, bitPattern);

        Assert.Equal(Overfloat.OverfloatClassification.Zero, roundTrip.Classification);
        Assert.True(roundTrip.Negative);
        Assert.Equal("-0", roundTrip.ToString());
    }

    [Fact]
    public void CompareTotal_OrdersNegativeZeroBeforePositiveZero()
    {
        var spec = new Overfloat.OverfloatSpecification(8, 23);
        var comparison = Overfloat.OverfloatMath.CompareTotal(
            Overfloat.OverfloatNumber.CreateZero(spec, true),
            Overfloat.OverfloatNumber.CreateZero(spec, false));

        Assert.True(comparison < 0);
    }
}
