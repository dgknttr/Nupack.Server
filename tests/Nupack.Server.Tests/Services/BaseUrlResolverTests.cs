using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Nupack.Server.Api.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class BaseUrlResolverTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<BaseUrlResolver>> _mockLogger;

    public BaseUrlResolverTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<BaseUrlResolver>>();
    }

    private BaseUrlResolver CreateResolver()
    {
        return new BaseUrlResolver(_mockConfiguration.Object, _mockEnvironment.Object, _mockLogger.Object);
    }

    [Fact]
    public void Resolve_WithValidHttpContext_ReturnsCorrectBaseUrl()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.example.com");

        // Act
        var result = resolver.Resolve(context);

        // Assert
        Assert.Equal("https://api.example.com", result);
    }

    [Fact]
    public void Resolve_WithHttpContextAndPort_ReturnsCorrectBaseUrl()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 5003);

        // Act
        var result = resolver.Resolve(context);

        // Assert
        Assert.Equal("http://localhost:5003", result);
    }

    // Note: Environment-specific tests are limited due to Moq extension method limitations with IsDevelopment()
    // The BaseUrlResolver environment behavior is tested in integration tests where real IWebHostEnvironment is available
    // Unit tests focus on HttpContext resolution which is the primary path in production
}
