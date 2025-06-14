using Nupack.Server.Api.Models;
using Nupack.Server.Api.Models.V3;
using Nupack.Server.Api.Services;
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

A modern NuGet V3 protocol compliant server built with .NET 8.

## NuGet V3 Protocol Support
- ✅ **Service Index**: `/v3/index.json`
- ✅ **Search Service**: `/v3/search`
- ✅ **Package Base Address**: `/v3-flatcontainer/`
- ✅ **Registration**: `/v3/registrations/`

## Features
- 🚀 **Full V3 Compliance**: Works with dotnet CLI, Visual Studio 2022+, nuget.exe
- 📦 **Semantic Versioning**: Proper SemVer support with prerelease handling
- 🔍 **Advanced Search**: Query, pagination, prerelease filtering
- 📊 **JSON Responses**: Modern JSON-based API (no XML/OData legacy)

## Usage
Configure your NuGet client to use: `http://localhost:5003/v3/index.json`

## Quick Test
Try these endpoints:
- GET `/v3/index.json` - Service discovery
- GET `/v3/search?q=TestPackage` - Search packages
- GET `/v3-flatcontainer/testpackage/index.json` - Package versions
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

// Configure package storage options
builder.Services.Configure<PackageStorageOptions>(
    builder.Configuration.GetSection(PackageStorageOptions.SectionName));

// Register our services
builder.Services.AddSingleton<IPackageStorageService, FileSystemPackageStorageService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IV3PackageService, V3PackageService>();
builder.Services.AddScoped<IBaseUrlResolver, BaseUrlResolver>();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nupack Server V3 API");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Nupack Server V3 API";
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
        c.DisplayRequestDuration();
    });
}

// Enable Swagger in all environments for demo purposes
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
app.MapGet("/v3-flatcontainer/{id}/{version}/{id2}.{version2}.nupkg", 
    async (string id, string version, string id2, string version2, IV3PackageService v3Service) =>
{
    // Validate that the path parameters match
    if (!string.Equals(id, id2, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(version, version2, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Package ID and version in path must match");
    }

    var stream = await v3Service.GetPackageContentAsync(id, version);
    if (stream == null)
        return Results.NotFound();

    return Results.File(stream, "application/octet-stream", $"{id}.{version}.nupkg");
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
app.MapPut("/v3/push", async (HttpContext context, IPackageService packageService) =>
{
    var form = await context.Request.ReadFormAsync();
    var packageFile = form.Files.GetFile("package");
    
    if (packageFile == null)
        return Results.BadRequest("Package file is required");

    var request = new PackageUploadRequest(packageFile);
    var result = await packageService.UploadPackageAsync(request);
    
    return result.Success ? Results.Created() : Results.BadRequest(result.Message);
})
.WithName("UploadPackage")
.WithSummary("Upload package")
.WithDescription("Upload a new package (.nupkg file)")
.Accepts<IFormFile>("multipart/form-data")
.Produces(201)
.Produces(400);

// Delete package
app.MapDelete("/v3/delete/{id}/{version}", async (string id, string version, IPackageService packageService) =>
{
    var result = await packageService.DeletePackageAsync(id, version);
    return result.Success ? Results.NoContent() : Results.NotFound();
})
.WithName("DeletePackage")
.WithSummary("Delete package")
.WithDescription("Delete a specific package version")
.Produces(204)
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

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Simple web UI
app.MapGet("/ui", () => Results.Content(GetSimpleWebUI(), "text/html"));

// Modern Frontend UI
app.MapGet("/frontend", () => Results.Content(GetModernFrontendUI(), "text/html"));

app.Run();

static string GetSimpleWebUI()
{
    return """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Nupack Server V3</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 40px; }
            .header { background: #0078d4; color: white; padding: 20px; border-radius: 8px; }
            .section { margin: 20px 0; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
            .endpoint { background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 4px; }
            code { background: #f0f0f0; padding: 2px 4px; border-radius: 3px; }
        </style>
    </head>
    <body>
        <div class="header">
            <h1>🚀 Nupack Server V3</h1>
            <p>Modern NuGet V3 Protocol Compliant Server</p>
        </div>

        <div class="section">
            <h2>📋 Service Index</h2>
            <p>Start here to discover all available services:</p>
            <div class="endpoint">
                <strong>GET</strong> <code>/v3/index.json</code>
            </div>
        </div>

        <div class="section">
            <h2>🔍 Search Packages</h2>
            <div class="endpoint">
                <strong>GET</strong> <code>/v3/search?q=package&prerelease=true</code>
            </div>
        </div>

        <div class="section">
            <h2>📦 Package Operations</h2>
            <div class="endpoint">
                <strong>GET</strong> <code>/v3-flatcontainer/{id}/index.json</code> - Get versions
            </div>
            <div class="endpoint">
                <strong>GET</strong> <code>/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg</code> - Download
            </div>
        </div>

        <div class="section">
            <h2>📊 Registration</h2>
            <div class="endpoint">
                <strong>GET</strong> <code>/v3/registrations/{id}/index.json</code> - Package metadata
            </div>
        </div>

        <div class="section">
            <h2>⚙️ Management</h2>
            <div class="endpoint">
                <strong>PUT</strong> <code>/v3/push</code> - Upload package
            </div>
            <div class="endpoint">
                <strong>DELETE</strong> <code>/v3/delete/{id}/{version}</code> - Delete package
            </div>
        </div>

        <div class="section">
            <h2>🔗 Quick Links</h2>
            <p><a href="/v3/index.json">Service Index</a> | <a href="/swagger">API Documentation</a> | <a href="/health">Health Check</a></p>
        </div>
    </body>
    </html>
    """;
}

static string GetModernFrontendUI()
{
    return """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Nupack Server - Modern UI</title>
        <script src="https://unpkg.com/react@18/umd/react.development.js"></script>
        <script src="https://unpkg.com/react-dom@18/umd/react-dom.development.js"></script>
        <script src="https://unpkg.com/@babel/standalone/babel.min.js"></script>
        <script src="https://cdn.tailwindcss.com"></script>
        <script>
            tailwind.config = {
                theme: {
                    extend: {
                        colors: {
                            'nuget-blue': '#004880',
                            'nuget-blue-light': '#0078d4',
                            'nuget-blue-dark': '#003366'
                        }
                    }
                }
            }
        </script>
        <style>
            .card { @apply bg-white rounded-lg shadow-sm border border-gray-200 p-6 transition-shadow duration-200 hover:shadow-md; }
            .btn-primary { @apply bg-nuget-blue hover:bg-nuget-blue-dark text-white font-medium py-2 px-4 rounded-lg transition-colors duration-200; }
            .btn-secondary { @apply bg-gray-200 hover:bg-gray-300 text-gray-800 font-medium py-2 px-4 rounded-lg transition-colors duration-200; }
        </style>
    </head>
    <body class="bg-gray-50">
        <div id="root"></div>

        <script type="text/babel">
            const { useState, useEffect } = React;

            // API functions
            const api = {
                async searchPackages(query = '', options = {}) {
                    const params = new URLSearchParams({
                        skip: (options.skip || 0).toString(),
                        take: (options.take || 20).toString(),
                        prerelease: (options.prerelease || false).toString(),
                    });
                    if (query) params.append('q', query);

                    const response = await fetch(`/v3/search?${params}`);
                    return response.json();
                },

                async getServiceIndex() {
                    const response = await fetch('/v3/index.json');
                    return response.json();
                },

                async getHealth() {
                    const response = await fetch('/health');
                    return response.json();
                }
            };

            // Package Card Component
            function PackageCard({ package: pkg }) {
                const isPrerelease = pkg.version.includes('-');

                const handleCopyCommand = () => {
                    const command = `dotnet add package ${pkg.id} --version ${pkg.version} --source "Nupack Server"`;
                    navigator.clipboard.writeText(command).then(() => {
                        alert('Command copied to clipboard!');
                    });
                };

                const handleDownload = () => {
                    const url = `/v3-flatcontainer/${pkg.id.toLowerCase()}/${pkg.version.toLowerCase()}/${pkg.id.toLowerCase()}.${pkg.version.toLowerCase()}.nupkg`;
                    window.open(url, '_blank');
                };

                return (
                    <div className="card group hover:shadow-lg transition-all duration-200">
                        <div className="flex items-start justify-between mb-4">
                            <div className="flex items-center space-x-3">
                                <div className="w-10 h-10 bg-gradient-to-br from-nuget-blue to-nuget-blue-light rounded-lg flex items-center justify-center">
                                    <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                                    </svg>
                                </div>
                                <div>
                                    <h3 className="font-semibold text-gray-900 group-hover:text-nuget-blue transition-colors duration-200">
                                        {pkg.id}
                                    </h3>
                                    <div className="flex items-center space-x-2 mt-1">
                                        <span className="text-sm text-gray-600">v{pkg.version}</span>
                                        {isPrerelease && (
                                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                                                Prerelease
                                            </span>
                                        )}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <p className="text-gray-600 text-sm mb-4 line-clamp-3">
                            {pkg.description || pkg.summary || 'No description available'}
                        </p>

                        <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                            <div className="flex items-center space-x-2">
                                <button
                                    onClick={handleCopyCommand}
                                    className="text-xs font-medium text-gray-600 hover:text-nuget-blue transition-colors duration-200"
                                >
                                    📋 Copy
                                </button>
                                <button
                                    onClick={handleDownload}
                                    className="text-xs font-medium text-gray-600 hover:text-green-600 transition-colors duration-200"
                                >
                                    ⬇️ Download
                                </button>
                            </div>
                            <span className="text-xs text-gray-400">
                                {pkg.totalDownloads || 0} downloads
                            </span>
                        </div>
                    </div>
                );
            }

            // Main App Component
            function App() {
                const [packages, setPackages] = useState([]);
                const [loading, setLoading] = useState(true);
                const [searchQuery, setSearchQuery] = useState('');
                const [stats, setStats] = useState({ total: 0, healthy: true });

                useEffect(() => {
                    loadData();
                }, []);

                const loadData = async () => {
                    try {
                        setLoading(true);

                        // Load packages and health
                        const [packagesResponse, healthResponse] = await Promise.all([
                            api.searchPackages('', { take: 12, prerelease: true }),
                            api.getHealth().catch(() => ({ status: 'unhealthy' }))
                        ]);

                        setPackages(packagesResponse.data || []);
                        setStats({
                            total: packagesResponse.totalHits || 0,
                            healthy: healthResponse.status === 'healthy'
                        });
                    } catch (error) {
                        console.error('Failed to load data:', error);
                    } finally {
                        setLoading(false);
                    }
                };

                const handleSearch = async (e) => {
                    e.preventDefault();
                    if (!searchQuery.trim()) return;

                    try {
                        setLoading(true);
                        const response = await api.searchPackages(searchQuery, { take: 12, prerelease: true });
                        setPackages(response.data || []);
                    } catch (error) {
                        console.error('Search failed:', error);
                    } finally {
                        setLoading(false);
                    }
                };

                return (
                    <div className="min-h-screen bg-gray-50">
                        {/* Header */}
                        <header className="bg-white shadow-sm border-b border-gray-200">
                            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                                <div className="flex justify-between items-center h-16">
                                    <div className="flex items-center space-x-3">
                                        <div className="w-8 h-8 bg-nuget-blue rounded-lg flex items-center justify-center">
                                            <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                                            </svg>
                                        </div>
                                        <div>
                                            <h1 className="text-xl font-bold text-gray-900">Nupack Server</h1>
                                            <p className="text-xs text-gray-500 -mt-1">Modern UI</p>
                                        </div>
                                    </div>

                                    <div className="flex items-center space-x-4">
                                        <div className="flex items-center space-x-2">
                                            <div className={`w-2 h-2 rounded-full ${stats.healthy ? 'bg-green-500' : 'bg-red-500'}`}></div>
                                            <span className="text-xs text-gray-500">
                                                {stats.healthy ? 'Healthy' : 'Offline'}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </header>

                        {/* Main Content */}
                        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                            {/* Hero Section */}
                            <div className="text-center mb-12">
                                <div className="flex justify-center mb-6">
                                    <div className="w-16 h-16 bg-gradient-to-br from-nuget-blue to-nuget-blue-light rounded-2xl flex items-center justify-center shadow-lg">
                                        <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                                        </svg>
                                    </div>
                                </div>
                                <h1 className="text-4xl font-bold text-gray-900 mb-4">
                                    Nupack Server Repository
                                </h1>
                                <p className="text-xl text-gray-600 max-w-2xl mx-auto mb-8">
                                    Modern React-based frontend for your private NuGet package repository.
                                </p>

                                {/* Search */}
                                <form onSubmit={handleSearch} className="max-w-lg mx-auto">
                                    <div className="flex">
                                        <input
                                            type="text"
                                            placeholder="Search packages..."
                                            value={searchQuery}
                                            onChange={(e) => setSearchQuery(e.target.value)}
                                            className="flex-1 px-4 py-3 border border-gray-300 rounded-l-lg focus:outline-none focus:ring-2 focus:ring-nuget-blue-light focus:border-transparent"
                                        />
                                        <button
                                            type="submit"
                                            className="btn-primary rounded-l-none"
                                            disabled={loading}
                                        >
                                            {loading ? '⏳' : '🔍'} Search
                                        </button>
                                    </div>
                                </form>
                            </div>

                            {/* Stats */}
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                                <div className="card text-center">
                                    <div className="text-3xl font-bold text-nuget-blue mb-2">{stats.total}</div>
                                    <div className="text-gray-600">Total Packages</div>
                                </div>
                                <div className="card text-center">
                                    <div className="text-3xl font-bold text-green-600 mb-2">✅</div>
                                    <div className="text-gray-600">V3 Protocol</div>
                                </div>
                                <div className="card text-center">
                                    <div className="text-3xl font-bold text-blue-600 mb-2">⚡</div>
                                    <div className="text-gray-600">Modern UI</div>
                                </div>
                            </div>

                            {/* Package Grid */}
                            <div>
                                <h2 className="text-2xl font-bold text-gray-900 mb-6">
                                    {searchQuery ? `Search Results for "${searchQuery}"` : 'Browse Packages'}
                                </h2>

                                {loading ? (
                                    <div className="text-center py-12">
                                        <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-nuget-blue"></div>
                                        <p className="text-gray-600 mt-4">Loading packages...</p>
                                    </div>
                                ) : packages.length > 0 ? (
                                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                                        {packages.map((pkg) => (
                                            <PackageCard key={`${pkg.id}-${pkg.version}`} package={pkg} />
                                        ))}
                                    </div>
                                ) : (
                                    <div className="text-center py-12">
                                        <svg className="w-16 h-16 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                                        </svg>
                                        <h3 className="text-xl font-medium text-gray-900 mb-2">
                                            {searchQuery ? 'No packages found' : 'No packages available'}
                                        </h3>
                                        <p className="text-gray-600">
                                            {searchQuery ? 'Try adjusting your search terms.' : 'Upload your first package to get started.'}
                                        </p>
                                    </div>
                                )}
                            </div>

                            {/* Quick Links */}
                            <div className="mt-12 card">
                                <h3 className="text-lg font-semibold text-gray-900 mb-4">Quick Links</h3>
                                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                                    <a href="/v3/index.json" target="_blank" className="text-nuget-blue hover:text-nuget-blue-dark transition-colors duration-200">
                                        📋 Service Index
                                    </a>
                                    <a href="/swagger" target="_blank" className="text-nuget-blue hover:text-nuget-blue-dark transition-colors duration-200">
                                        📚 API Docs
                                    </a>
                                    <a href="/health" target="_blank" className="text-nuget-blue hover:text-nuget-blue-dark transition-colors duration-200">
                                        ❤️ Health Check
                                    </a>
                                    <a href="/ui" className="text-nuget-blue hover:text-nuget-blue-dark transition-colors duration-200">
                                        🔧 Simple UI
                                    </a>
                                </div>
                            </div>
                        </main>

                        {/* Footer */}
                        <footer className="bg-white border-t border-gray-200 mt-12">
                            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                                <div className="text-center text-gray-500">
                                    <p>🚀 Nupack Server V3 - Modern React Frontend</p>
                                    <p className="text-sm mt-2">Built with React, Tailwind CSS, and ❤️</p>
                                </div>
                            </div>
                        </footer>
                    </div>
                );
            }

            // Render the app
            ReactDOM.render(<App />, document.getElementById('root'));
        </script>
    </body>
    </html>
    """;
}

// Make Program class accessible for testing
public partial class Program { }