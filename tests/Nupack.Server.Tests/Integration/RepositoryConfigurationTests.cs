using FluentAssertions;
using Xunit;

namespace Nupack.Server.Tests.Integration;

public class RepositoryConfigurationTests
{
    [Fact]
    public void DockerCompose_UsesNamedVolumesForContainerData()
    {
        var repositoryRoot = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(repositoryRoot, "docker-compose.yml"));

        compose.Should().Contain("- nupack-data:/app/data");
        compose.Should().NotContain("- ./data:/app/data");
        compose.Should().Contain("\nvolumes:\n  nupack-data:\n");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "docker-compose.yml")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root containing docker-compose.yml.");
    }
}
