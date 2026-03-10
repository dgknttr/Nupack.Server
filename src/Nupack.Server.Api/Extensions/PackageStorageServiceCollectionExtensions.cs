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

        services.AddSingleton<FileSystemPackageStorageService>();
        services.AddSingleton<S3PackageStorageService>();
        services.AddSingleton<IPackageStorageService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PackageStorageOptions>>().Value;
            return options.GetSelectedProvider() switch
            {
                PackageStorageProvider.FileSystem => serviceProvider.GetRequiredService<FileSystemPackageStorageService>(),
                PackageStorageProvider.S3 => serviceProvider.GetRequiredService<S3PackageStorageService>(),
                _ => throw new InvalidOperationException($"Unsupported PackageStorage provider '{options.GetSelectedProvider()}'.")
            };
        });

        services.AddHostedService<PackageStorageWarmupHostedService>();
        return services;
    }
}
