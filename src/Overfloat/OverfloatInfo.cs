using System.Reflection;

namespace Overfloat;

public static class OverfloatInfo
{
    private static readonly Lazy<Version> ParsedVersion = new(ParseVersion);

    public static int MajorVersion => ParsedVersion.Value.Major;

    public static int MinorVersion => ParsedVersion.Value.Minor;

    public static int PatchVersion => ParsedVersion.Value.Build >= 0 ? ParsedVersion.Value.Build : 0;

    public static string Version => $"{MajorVersion}.{MinorVersion}.{PatchVersion}";

    private static Version ParseVersion()
    {
        var assembly = typeof(OverfloatInfo).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var normalized = informationalVersion.Split('+', 2)[0].Split('-', 2)[0];
            if (System.Version.TryParse(normalized, out var parsedInformationalVersion))
            {
                return parsedInformationalVersion;
            }
        }

        var assemblyVersion = assembly.GetName().Version;
        return assemblyVersion ?? new Version(0, 0, 0);
    }
}
