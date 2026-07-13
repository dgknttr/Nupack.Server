using System.Globalization;

namespace Nupack.Server.Api.Models;

public sealed class PackageHealthOptions
{
    public const string SectionName = "PackageHealth";
    public const string ReadinessTimeoutSecondsKey = "ReadinessTimeoutSeconds";
    public const int DefaultReadinessTimeoutSeconds = 5;
    private const int MaximumReadinessTimeoutSeconds = 300;

    public static TimeSpan ResolveReadinessTimeout(string? configuredSeconds)
    {
        if (!int.TryParse(configuredSeconds, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            seconds <= 0 ||
            seconds > MaximumReadinessTimeoutSeconds)
        {
            seconds = DefaultReadinessTimeoutSeconds;
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
