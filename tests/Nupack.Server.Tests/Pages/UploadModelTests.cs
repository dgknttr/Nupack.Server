extern alias NupackWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebPages = NupackWeb::Nupack.Server.Web.Pages;
using Xunit;

namespace Nupack.Server.Tests.Pages;

public class UploadModelTests
{
    [Fact]
    public async Task OnPostAsync_WithUnauthorizedProblemDetails_ReturnsFriendlyMessage()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    title = "Package write authorization required",
                    status = 401,
                    detail = "A valid X-NuGet-ApiKey header is required for package write operations."
                }),
                Encoding.UTF8,
                "application/problem+json")
        }));

        var model = CreateModel(httpClient);
        model.PackageFile = CreatePackageFile();

        await model.OnPostAsync();

        model.IsSuccess.Should().BeFalse();
        model.Message.Should().Be("A valid X-NuGet-ApiKey header is required for package write operations.");
    }

    [Fact]
    public async Task OnPostAsync_WithApiKey_SendsHeaderToApi()
    {
        HttpRequestMessage? capturedRequest = null;
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.Created);
        }));

        var model = CreateModel(httpClient);
        model.ApiKey = "secret-key";
        model.PackageFile = CreatePackageFile();

        await model.OnPostAsync();

        model.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.TryGetValues("X-NuGet-ApiKey", out var headerValues).Should().BeTrue();
        headerValues.Should().ContainSingle().Which.Should().Be("secret-key");
    }

    private static WebPages.UploadModel CreateModel(HttpClient httpClient)
    {
        var logger = Mock.Of<ILogger<WebPages.UploadModel>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NuGetServer:BaseUrl"] = "http://localhost:5003"
            })
            .Build();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new WebPages.UploadModel(logger, configuration, httpClientFactory.Object);
    }

    private static IFormFile CreatePackageFile()
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "package", "TestPackage.1.0.0.nupkg");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
