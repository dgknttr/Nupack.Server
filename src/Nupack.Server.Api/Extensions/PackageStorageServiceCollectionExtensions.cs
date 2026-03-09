using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using Nupack.Server.Storage.FileSystem;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.S3;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Api.Extensions;

public static class PackageStorageServiceCollectionExtensions
{
    public static IServiceCollection AddNupackStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<PackageStorageOptions>, PackageStorageOptionsValidator>();
        services.AddOptions<PackageStorageOptions>()
            .Bind(configuration.GetSection(PackageStorageOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<PackageArchiveMetadataReader>();

        var provider = GetConfiguredProvider(configuration);
        switch (provider)
        {
            case PackageStorageProvider.FileSystem:
                services.AddSingleton<IPackageStorageService, FileSystemPackageStorageService>();
                break;
            case PackageStorageProvider.S3:
                services.AddSingleton<IAmazonS3>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<PackageStorageOptions>>().Value.S3;
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region!),
                        ForcePathStyle = options.ForcePathStyle
                    };

                    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                    {
                        config.ServiceURL = options.ServiceUrl;
                    }

                    var credentials = new BasicAWSCredentials(options.AccessKey!, options.SecretKey!);
                    return new AmazonS3Client(credentials, config);
                });
                services.AddSingleton<IPackageStorageService, S3PackageStorageService>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported PackageStorage provider '{provider}'.");
        }

        services.AddHostedService<PackageStorageWarmupHostedService>();
        return services;
    }

    private static PackageStorageProvider GetConfiguredProvider(IConfiguration configuration)
    {
        var options = configuration.GetSection(PackageStorageOptions.SectionName).Get<PackageStorageOptions>() ?? new PackageStorageOptions();
        return options.GetSelectedProvider();
    }
}
