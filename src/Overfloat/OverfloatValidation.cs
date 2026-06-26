namespace Overfloat;

public static class OverfloatValidation
{
    public static OverfloatStatus ValidateSpecification(int exponentBits, int mantissaBits)
    {
        if (exponentBits < 2 || exponentBits > 30)
        {
            return OverfloatStatus.InvalidArgument;
        }

        if (mantissaBits < 1)
        {
            return OverfloatStatus.InvalidArgument;
        }

        return OverfloatStatus.Success;
    }
}
