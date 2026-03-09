using Microsoft.Extensions.Options;
using Nupack.Server.Storage.Models;

namespace Nupack.Server.Storage;

public sealed class PackageStorageOptionsValidator : IValidateOptions<PackageStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, PackageStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.GetSelectedProvider() switch
        {
            PackageStorageProvider.FileSystem => ValidateOptionsResult.Success,
            PackageStorageProvider.S3 => ValidateS3(options.S3),
            _ => ValidateOptionsResult.Fail("Unsupported PackageStorage provider.")
        };
    }

    private static ValidateOptionsResult ValidateS3(S3PackageStorageOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            failures.Add("PackageStorage:S3:BucketName is required when Provider is S3.");
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add("PackageStorage:S3:Region is required when Provider is S3.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessKey))
        {
            failures.Add("PackageStorage:S3:AccessKey is required when Provider is S3.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            failures.Add("PackageStorage:S3:SecretKey is required when Provider is S3.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
