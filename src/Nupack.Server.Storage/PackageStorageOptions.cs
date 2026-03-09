namespace Nupack.Server.Storage.Models;

public enum PackageStorageProvider
{
    FileSystem,
    S3
}

public sealed class PackageStorageOptions
{
    public const string SectionName = "PackageStorage";

    public PackageStorageProvider? Provider { get; set; }

    // Legacy shorthand preserved for backward compatibility.
    public string? BasePath { get; set; }

    public FileSystemPackageStorageOptions FileSystem { get; set; } = new();

    public S3PackageStorageOptions S3 { get; set; } = new();

    public PackageStorageProvider GetSelectedProvider()
        => Provider ?? PackageStorageProvider.FileSystem;

    public FileSystemPackageStorageOptions GetResolvedFileSystemOptions()
    {
        if (string.IsNullOrWhiteSpace(FileSystem.BasePath) && !string.IsNullOrWhiteSpace(BasePath))
        {
            return new FileSystemPackageStorageOptions
            {
                BasePath = BasePath
            };
        }

        return FileSystem;
    }
}

public sealed class FileSystemPackageStorageOptions
{
    public string? BasePath { get; set; }

    public string GetResolvedPath(string? webRootPath, string contentRootPath)
    {
        var basePath = BasePath;

        if (string.IsNullOrWhiteSpace(basePath))
        {
            return Path.Combine(webRootPath ?? contentRootPath, "packages");
        }

        basePath = ExpandEnvironmentVariables(basePath);

        if (Path.IsPathRooted(basePath))
        {
            return Path.GetFullPath(basePath);
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, basePath));
    }

    private static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var result = path;
        var startIndex = 0;

        while (true)
        {
            var start = result.IndexOf("${", startIndex, StringComparison.Ordinal);
            if (start == -1)
            {
                break;
            }

            var end = result.IndexOf("}", start + 2, StringComparison.Ordinal);
            if (end == -1)
            {
                break;
            }

            var variableName = result.Substring(start + 2, end - start - 2);
            var variableValue = Environment.GetEnvironmentVariable(variableName) ?? string.Empty;

            result = result.Substring(0, start) + variableValue + result.Substring(end + 1);
            startIndex = start + variableValue.Length;
        }

        return result;
    }
}

public sealed class S3PackageStorageOptions
{
    public string? BucketName { get; set; }

    public string? Region { get; set; }

    public string? ServiceUrl { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? Prefix { get; set; }

    public bool ForcePathStyle { get; set; }

    public string GetNormalizedPrefix()
    {
        var prefix = Prefix?.Trim() ?? string.Empty;
        prefix = prefix.Trim('/');

        return string.IsNullOrWhiteSpace(prefix)
            ? string.Empty
            : prefix + "/";
    }
}
