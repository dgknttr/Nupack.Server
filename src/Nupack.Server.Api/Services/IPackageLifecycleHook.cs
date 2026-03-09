using Nupack.Server.Api.Models;
using Nupack.Server.Storage.Models;

namespace Nupack.Server.Api.Services;

public interface IPackageLifecycleHook
{
    Task OnPackageUploadedAsync(PackageMetadata metadata, CancellationToken cancellationToken = default);
    Task OnPackageDeletedAsync(string id, string version, CancellationToken cancellationToken = default);
}

public sealed class NullPackageLifecycleHook : IPackageLifecycleHook
{
    public Task OnPackageUploadedAsync(PackageMetadata metadata, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnPackageDeletedAsync(string id, string version, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
