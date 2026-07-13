using Nupack.Server.Api.Models;
using Nupack.Server.Api.Models.V3;
using Nupack.Server.Api.Extensions;
using Nupack.Server.Api.Services;
using Nupack.Server.Storage;
using Nupack.Server.Storage.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "Nupack Server V3 API",
        Version = "v3.0.0",
        Description = @"
# Nupack Server V3 API

A lightweight NuGet V3 server reference implementation built with .NET 9.

## Current Focus
- Reference implementation for self-hosted feeds and learning projects
- Conservative protocol surface with file system storage
- Separate Razor Pages web UI as the only supported user-facing experience

## Supported V3 Endpoints
- Supported: `/v3/index.json`, `/v3/search`, `/v3-flatcontainer/*`, `/v3/registrations/*`
- Supported: `PUT /v3/push`, `DELETE /v3/delete/{id}/{version}`
- Supported: `/health/live`, `/health/ready`, and readiness alias `/health`

## Notes
- Search, read, and download endpoints are anonymous
- `X-NuGet-ApiKey` uses `PackageSecurity:PublishApiKey` for push and `PackageSecurity:DeleteApiKey` for delete; `WriteApiKey` is a 0.x compatibility fallback
- Missing applicable write credentials are allowed by default only in Development; non-Development deployments must configure the operation-specific key or opt in with `PackageSecurity:AllowAnonymousWrites`
- Rate limiting is not built in yet
- Use `/swagger` and the repository docs for the current support matrix
"
    });

    // Configure for better API documentation
    c.EnableAnnotations();
    c.DescribeAllParametersInCamelCase();

    // Add XML comments if available (optional)
    try
    {
        var xmlFile = Path.Combine(AppContext.BaseDirectory, "Nupack.Server.Api.xml");
        if (File.Exists(xmlFile))
        {
            c.IncludeXmlComments(xmlFile, true);
        }
    }
    catch
    {
        // Ignore XML comments errors
    }
});

// Configure JSON options for V3 API
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// Configure package storage and security options
var packageUploadOptions = builder.Configuration
    .GetSection(PackageUploadOptions.SectionName)
    .Get<PackageUploadOptions>() ?? new PackageUploadOptions();

builder.Services.Configure<PackageUploadOptions>(
    builder.Configuration.GetSection(PackageUploadOptions.SectionName));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = packageUploadOptions.GetResolvedMaxPackageSizeBytes();
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = packageUploadOptions.GetRequestBodySizeLimitBytes();
});

builder.Services.Configure<PackageSecurityOptions>(
    builder.Configuration.GetSection(PackageSecurityOptions.SectionName));

// Register our services
builder.Services.AddNupackStorage(builder.Configuration);
var readinessTimeout = PackageHealthOptions.ResolveReadinessTimeout(
    builder.Configuration[$"{PackageHealthOptions.SectionName}:{PackageHealthOptions.ReadinessTimeoutSecondsKey}"]);
builder.Services.AddHealthChecks()
    .AddCheck<PackageStorageHealthCheck>("package-storage", tags: ["ready"], timeout: readinessTimeout);
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IV3PackageService, V3PackageService>();
builder.Services.AddScoped<IBaseUrlResolver, BaseUrlResolver>();
builder.Services.AddScoped<IPackageEndpointAuthorizer, HeaderApiKeyPackageEndpointAuthorizer>();
builder.Services.AddScoped<IPackageUploadValidator, DefaultPackageUploadValidator>();
builder.Services.AddScoped<IPackageLifecycleHook, NullPackageLifecycleHook>();

// Add logging
builder.Services.AddLogging();

// Add CORS for web clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nupack Server V3 API");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Nupack Server V3 API";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

app.UseCors();

// Note: Base URL resolution is now handled by IBaseUrlResolver service
// This eliminates the need for this helper method and centralizes the logic

// ============================================================================
// NuGet V3 API Endpoints
// ============================================================================

// V3 Service Index - Entry point for NuGet V3 protocol
app.MapGet("/v3/index.json", (HttpContext context, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var serviceIndex = v3Service.GetServiceIndex(baseUrl);
    return Results.Json(serviceIndex);
})
.WithName("GetServiceIndex")
.WithSummary("Get NuGet V3 Service Index")
.WithDescription("Returns the service index that lists all available V3 services")
.Produces<ServiceIndex>(200, "application/json");

// Search Service
app.MapGet("/v3/search", async (HttpContext context, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver,
    string? q = null, int skip = 0, int take = 20, bool prerelease = false, string? semVerLevel = null) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var result = await v3Service.SearchPackagesAsync(q, skip, take, prerelease, semVerLevel, baseUrl);
    return Results.Json(result);
})
.WithName("SearchPackages")
.WithSummary("Search packages")
.WithDescription("Search for packages using the NuGet V3 search protocol")
.Produces<SearchResponse>(200, "application/json");

// Package Base Address (Flat Container) - Package versions index
app.MapGet("/v3-flatcontainer/{id}/index.json", async (string id, IV3PackageService v3Service) =>
{
    var result = await v3Service.GetPackageVersionsAsync(id);
    if (result == null)
        return Results.NotFound();
    
    return Results.Json(result);
})
.WithName("GetPackageVersions")
.WithSummary("Get package versions")
.WithDescription("Get all available versions for a package")
.Produces<PackageVersionsIndex>(200, "application/json")
.Produces(404);

// Package Base Address - Download package
app.MapGet("/v3-flatcontainer/{id}/{version}/{fileName}",
    async (string id, string version, string fileName, IV3PackageService v3Service) =>
{
    var expectedFileName = $"{id}.{version}.nupkg";
    if (!string.Equals(fileName, expectedFileName, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Package ID and version in path must match the requested file name");
    }

    var stream = await v3Service.GetPackageContentAsync(id, version);
    if (stream == null)
        return Results.NotFound();

    return Results.File(stream, "application/octet-stream", expectedFileName);
})
.WithName("DownloadPackage")
.WithSummary("Download package")
.WithDescription("Download a specific package version")
.Produces(200, contentType: "application/octet-stream")
.Produces(400)
.Produces(404);

// Package Base Address - Get package manifest (.nuspec)
app.MapGet("/v3-flatcontainer/{id}/{version}/{id2}.nuspec", 
    async (string id, string version, string id2, IV3PackageService v3Service) =>
{
    if (!string.Equals(id, id2, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Package ID in path must match");
    }

    var stream = await v3Service.GetPackageManifestAsync(id, version);
    if (stream == null)
        return Results.NotFound();

    return Results.File(stream, "application/xml", $"{id}.nuspec");
})
.WithName("GetPackageManifest")
.WithSummary("Get package manifest")
.WithDescription("Get the .nuspec manifest for a specific package version")
.Produces(200, contentType: "application/xml")
.Produces(400)
.Produces(404);

// Registration - Package registration index
app.MapGet("/v3/registrations/{id}/index.json", async (HttpContext context, string id, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var result = await v3Service.GetRegistrationIndexAsync(id, baseUrl);
    if (result == null)
        return Results.NotFound();

    return Results.Json(result);
})
.WithName("GetRegistrationIndex")
.WithSummary("Get package registration index")
.WithDescription("Get the registration index for a package (all versions metadata)")
.Produces<RegistrationIndex>(200, "application/json")
.Produces(404);

// Registration - Package registration page
app.MapGet("/v3/registrations/{id}/page/{lower}/{upper}.json",
    async (HttpContext context, string id, string lower, string upper, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var result = await v3Service.GetRegistrationPageAsync(id, lower, upper, baseUrl);
    if (result == null)
        return Results.NotFound();

    return Results.Json(result);
})
.WithName("GetRegistrationPage")
.WithSummary("Get package registration page")
.WithDescription("Get a page of package registrations within a version range")
.Produces<RegistrationPage>(200, "application/json")
.Produces(404);

// Registration - Package registration leaf
app.MapGet("/v3/registrations/{id}/{version}.json", async (HttpContext context, string id, string version, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var result = await v3Service.GetRegistrationLeafAsync(id, version, baseUrl);
    if (result == null)
        return Results.NotFound();

    return Results.Json(result);
})
.WithName("GetRegistrationLeaf")
.WithSummary("Get package registration leaf")
.WithDescription("Get detailed metadata for a specific package version")
.Produces<RegistrationLeaf>(200, "application/json")
.Produces(404);

// ============================================================================
// Package Management Endpoints (Upload/Delete)
// ============================================================================

// Upload package
app.MapPut("/v3/push", async (HttpContext context, IPackageService packageService, IPackageEndpointAuthorizer authorizer) =>
{
    IFormCollection form;
    try
    {
        form = await context.Request.ReadFormAsync();
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest($"Package file size cannot exceed {packageUploadOptions.GetMaxPackageSizeDisplay()}.");
    }

    var packageFile = form.Files.GetFile("package");

    if (packageFile == null)
        return Results.BadRequest("Package file is required");

    var authorization = await authorizer.AuthorizeUploadAsync(context, packageFile, context.RequestAborted);
    if (!authorization.IsAllowed)
    {
        return Results.Problem(statusCode: authorization.StatusCode, detail: authorization.Message, title: "Package write authorization required");
    }

    var request = new PackageUploadRequest(packageFile);
    var result = await packageService.UploadPackageAsync(request);

    return result.Success ? Results.Created() : Results.BadRequest(result.Message);
})
.WithName("UploadPackage")
.WithSummary("Upload package")
.WithDescription("Upload a new package (.nupkg file)")
.Accepts<IFormFile>("multipart/form-data")
.Produces(201)
.Produces(401)
.Produces(400);

// Delete package
app.MapDelete("/v3/delete/{id}/{version}", async (HttpContext context, string id, string version, IPackageService packageService, IPackageEndpointAuthorizer authorizer) =>
{
    var authorization = await authorizer.AuthorizeDeleteAsync(context, id, version, context.RequestAborted);
    if (!authorization.IsAllowed)
    {
        return Results.Problem(statusCode: authorization.StatusCode, detail: authorization.Message, title: "Package write authorization required");
    }

    var result = await packageService.DeletePackageAsync(id, version);
    return result.Success ? Results.NoContent() : Results.NotFound();
})
.WithName("DeletePackage")
.WithSummary("Delete package")
.WithDescription("Delete a specific package version")
.Produces(204)
.Produces(401)
.Produces(404);

// ============================================================================
// Web UI and Health Check
// ============================================================================

// Root endpoint - return service index directly
app.MapGet("/", (HttpContext context, IV3PackageService v3Service, IBaseUrlResolver baseUrlResolver) =>
{
    var baseUrl = baseUrlResolver.Resolve(context);
    var serviceIndex = v3Service.GetServiceIndex(baseUrl);
    return Results.Json(serviceIndex);
});

// Health checks: liveness never probes external dependencies; readiness probes the selected storage provider.
var livenessOptions = new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
};
var readinessOptions = new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
};
app.MapHealthChecks("/health/live", livenessOptions);
app.MapHealthChecks("/health/ready", readinessOptions);
app.MapHealthChecks("/health", readinessOptions);

app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString().ToLowerInvariant(),
        timestamp = DateTime.UtcNow
    });
}

// Make Program class accessible for testing
public partial class Program { }
