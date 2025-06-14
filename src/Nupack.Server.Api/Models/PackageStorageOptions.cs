namespace Nupack.Server.Api.Models;

/// <summary>
/// Configuration options for package storage
/// </summary>
public class PackageStorageOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "PackageStorage";

    /// <summary>
    /// Base path where packages will be stored.
    /// If not specified, defaults to "packages" folder in WebRootPath or ContentRootPath.
    /// Supports both absolute and relative paths.
    /// Environment variables can be used with ${VARIABLE_NAME} syntax.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Gets the resolved storage path, handling cross-platform compatibility
    /// </summary>
    /// <param name="webRootPath">Web root path from IWebHostEnvironment</param>
    /// <param name="contentRootPath">Content root path from IWebHostEnvironment</param>
    /// <returns>Resolved absolute path for package storage</returns>
    public string GetResolvedPath(string? webRootPath, string contentRootPath)
    {
        var basePath = BasePath;

        // If no custom path specified, use default
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return Path.Combine(webRootPath ?? contentRootPath, "packages");
        }

        // Expand environment variables
        basePath = ExpandEnvironmentVariables(basePath);

        // If absolute path, use as-is
        if (Path.IsPathRooted(basePath))
        {
            return Path.GetFullPath(basePath);
        }

        // If relative path, combine with content root
        return Path.GetFullPath(Path.Combine(contentRootPath, basePath));
    }

    /// <summary>
    /// Expands environment variables in the path using ${VARIABLE_NAME} syntax
    /// </summary>
    private static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Replace ${VARIABLE_NAME} with environment variable values
        var result = path;
        var startIndex = 0;

        while (true)
        {
            var start = result.IndexOf("${", startIndex, StringComparison.Ordinal);
            if (start == -1) break;

            var end = result.IndexOf("}", start + 2, StringComparison.Ordinal);
            if (end == -1) break;

            var variableName = result.Substring(start + 2, end - start - 2);
            var variableValue = Environment.GetEnvironmentVariable(variableName) ?? "";

            result = result.Substring(0, start) + variableValue + result.Substring(end + 1);
            startIndex = start + variableValue.Length;
        }

        return result;
    }
}
