namespace Overfloat;

public static class OverfloatInfo
{
    public const int MajorVersion = 0;
    public const int MinorVersion = 1;
    public const int PatchVersion = 0;

    public static string Version => $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
}
