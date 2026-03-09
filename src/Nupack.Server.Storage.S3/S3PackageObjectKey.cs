namespace Nupack.Server.Storage.S3;

public static class S3PackageObjectKey
{
    public static string Build(string prefix, string packageId, string version)
    {
        var normalizedPrefix = prefix.Trim().Trim('/');
        var basePath = string.IsNullOrWhiteSpace(normalizedPrefix)
            ? string.Empty
            : normalizedPrefix + "/";

        var idLower = packageId.ToLowerInvariant();
        var versionLower = version.ToLowerInvariant();
        return $"{basePath}{idLower}/{versionLower}/{idLower}.{versionLower}.nupkg";
    }

    public static bool IsPackageObject(string key)
        => key.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);
}

