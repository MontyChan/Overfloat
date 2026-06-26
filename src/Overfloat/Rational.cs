using System.Numerics;

namespace Overfloat;

internal readonly struct Rational : IComparable<Rational>
{
    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
        {
            throw new DivideByZeroException();
        }

        if (denominator.Sign < 0)
        {
            numerator = BigInteger.Negate(numerator);
            denominator = BigInteger.Negate(denominator);
        }

        if (numerator.IsZero)
        {
            Numerator = BigInteger.Zero;
            Denominator = BigInteger.One;
            return;
        }

        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    public BigInteger Numerator { get; }

    public BigInteger Denominator { get; }

    public bool IsZero => Numerator.IsZero;

    public int Sign => Numerator.Sign;

    public Rational Abs() => Sign < 0 ? new Rational(BigInteger.Abs(Numerator), Denominator) : this;

    public int CompareTo(Rational other)
        => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

    public static Rational operator +(Rational left, Rational right)
        => new(left.Numerator * right.Denominator + right.Numerator * left.Denominator, left.Denominator * right.Denominator);

    public static Rational operator -(Rational left, Rational right)
        => new(left.Numerator * right.Denominator - right.Numerator * left.Denominator, left.Denominator * right.Denominator);

    public static Rational operator *(Rational left, Rational right)
        => new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static Rational operator /(Rational left, Rational right)
    {
        if (right.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new Rational(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
    }
}
