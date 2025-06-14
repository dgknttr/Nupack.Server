# 🚀 Nupack Server V3 API Guide

## Overview

The Nupack Server has been **completely modernized** to support the **NuGet V3 protocol**, making it fully compatible with:

- ✅ **dotnet CLI** (dotnet add package, dotnet restore)
- ✅ **Visual Studio 2022+** (Package Manager UI)
- ✅ **nuget.exe** (NuGet command-line tool)
- ✅ **Modern NuGet clients** (VS Code, Rider, etc.)

## 🏗️ Architecture

The V3 implementation follows a **clean, modular architecture**:

```
┌─────────────────────────────────────────────────────────────┐
│                    NuGet V3 Protocol                       │
├─────────────────────────────────────────────────────────────┤
│  Service Index → Search → Flat Container → Registration    │
├─────────────────────────────────────────────────────────────┤
│                   ASP.NET Core 8                           │
│                   Minimal APIs                             │
├─────────────────────────────────────────────────────────────┤
│              V3PackageService                              │
│              (Business Logic)                              │
├─────────────────────────────────────────────────────────────┤
│           FileSystemPackageStorageService                  │
│              (Storage Layer)                               │
└─────────────────────────────────────────────────────────────┘
```

## 🔗 V3 API Endpoints

### 1. Service Index (Entry Point)
**GET** `/v3/index.json`

The **discovery endpoint** that lists all available V3 services.

```json
{
  "version": "3.0.0",
  "resources": [
    {
      "@id": "http://localhost:5003/v3-flatcontainer/",
      "@type": "PackageBaseAddress/3.0.0",
      "comment": "Package download and metadata"
    },
    {
      "@id": "http://localhost:5003/v3/search",
      "@type": "SearchQueryService",
      "comment": "Package search"
    },
    {
      "@id": "http://localhost:5003/v3/registrations/",
      "@type": "RegistrationsBaseUrl",
      "comment": "Package registration metadata"
    }
  ]
}
```

### 2. Search Service
**GET** `/v3/search`

**Parameters:**
- `q` - Search query (package name, description, tags)
- `skip` - Number of results to skip (pagination)
- `take` - Number of results to return (default: 20)
- `prerelease` - Include prerelease versions (default: false)
- `semVerLevel` - Semantic version level

**Example:**
```bash
GET /v3/search?q=TestPackage&prerelease=true&take=10
```

**Response:**
```json
{
  "totalHits": 1,
  "data": [
    {
      "@id": "http://localhost:5003/v3/registrations/testpackage/index.json",
      "@type": "Package",
      "id": "TestPackage",
      "version": "1.0.0",
      "description": "A test package for Nupack Server",
      "authors": ["Development Team"],
      "tags": ["test", "demo", "nuget"],
      "versions": [
        {
          "version": "1.0.0",
          "downloads": 0,
          "@id": "http://localhost:5003/v3/registrations/testpackage/1.0.0.json"
        }
      ]
    }
  ]
}
```

### 3. Package Base Address (Flat Container)

#### Get Package Versions
**GET** `/v3-flatcontainer/{id}/index.json`

Returns all available versions for a package.

**Example:**
```bash
GET /v3-flatcontainer/testpackage/index.json
```

**Response:**
```json
{
  "versions": ["1.0.0", "1.1.0-beta.1", "2.0.0-alpha.1"]
}
```

#### Download Package
**GET** `/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg`

Downloads the actual package file.

**Example:**
```bash
GET /v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg
```

#### Get Package Manifest
**GET** `/v3-flatcontainer/{id}/{version}/{id}.nuspec`

Returns the package manifest (.nuspec file).

### 4. Registration

#### Registration Index
**GET** `/v3/registrations/{id}/index.json`

Returns comprehensive metadata for all versions of a package.

**Example:**
```bash
GET /v3/registrations/testpackage/index.json
```

#### Registration Leaf
**GET** `/v3/registrations/{id}/{version}.json`

Returns detailed metadata for a specific package version.

**Example:**
```bash
GET /v3/registrations/testpackage/1.0.0.json
```

## 🛠️ Package Management

### Upload Package
**PUT** `/v3/push`

Upload a new package (.nupkg file).

```bash
curl -X PUT -F "package=@MyPackage.1.0.0.nupkg" http://localhost:5003/v3/push
```

### Delete Package
**DELETE** `/v3/delete/{id}/{version}`

Delete a specific package version.

```bash
curl -X DELETE http://localhost:5003/v3/delete/MyPackage/1.0.0
```

## 🔧 Configuration

### NuGet Client Configuration

#### dotnet CLI
```bash
dotnet nuget add source "http://localhost:5003/v3/index.json" --name "Nupack Server"
```

#### nuget.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Nupack Server" value="http://localhost:5003/v3/index.json" />
  </packageSources>
</configuration>
```

#### Visual Studio
1. Tools → Options → NuGet Package Manager → Package Sources
2. Add new source:
   - **Name:** Nupack Server
   - **Source:** `http://localhost:5003/v3/index.json`

## 🧪 Testing

### Quick Test Commands

```bash
# Test service index
curl http://localhost:5003/v3/index.json

# Search packages
curl "http://localhost:5003/v3/search?q=TestPackage"

# Get package versions
curl http://localhost:5003/v3-flatcontainer/testpackage/index.json

# Download package
curl -o package.nupkg http://localhost:5003/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg
```

### dotnet CLI Testing

```bash
# Add package source
dotnet nuget add source "http://localhost:5003/v3/index.json" --name "Nupack Server"

# Search packages
dotnet package search TestPackage --source "Nupack Server"

# Add package to project
dotnet add package TestPackage --source "Nupack Server"
```

## 🚀 Key Features

### ✅ Full V3 Protocol Compliance
- **Service Discovery:** Automatic endpoint discovery via service index
- **JSON Responses:** Modern JSON-based API (no XML/OData legacy)
- **Semantic Versioning:** Proper SemVer support with prerelease handling
- **Pagination:** Efficient result paging for large package catalogs

### ✅ Modern Architecture
- **ASP.NET Core 8:** Latest .NET framework with Minimal APIs
- **Dependency Injection:** Clean, testable service architecture
- **Async/Await:** Non-blocking I/O operations
- **Error Handling:** Comprehensive error responses

### ✅ Developer Experience
- **Swagger Documentation:** Interactive API documentation
- **Web UI:** Simple web interface for package browsing
- **Health Checks:** Monitoring and diagnostics endpoints
- **CORS Support:** Cross-origin requests for web clients

## 🔄 Migration from V2

The server maintains **backward compatibility** with V2 clients while providing full V3 support:

| Feature | V2 (Legacy) | V3 (Modern) |
|---------|-------------|-------------|
| Protocol | OData/XML | JSON REST |
| Discovery | Fixed endpoints | Service index |
| Search | Limited | Advanced with filters |
| Metadata | Basic | Rich with dependencies |
| Performance | Slower | Optimized |
| Client Support | Limited | Universal |

## 📊 Performance

The V3 implementation provides significant performance improvements:

- **Faster Search:** JSON parsing vs XML processing
- **Efficient Caching:** In-memory package metadata cache
- **Reduced Bandwidth:** Compressed JSON responses
- **Parallel Processing:** Async operations throughout

## 🔒 Security

- **Input Validation:** All endpoints validate input parameters
- **Error Handling:** Secure error messages without information leakage
- **CORS Configuration:** Configurable cross-origin policies
- **Package Validation:** .nupkg file validation during upload

## 🌐 Production Deployment

For production use, consider:

1. **HTTPS:** Enable SSL/TLS encryption
2. **Authentication:** Implement API key or OAuth authentication
3. **Rate Limiting:** Add rate limiting for API endpoints
4. **Monitoring:** Set up logging and metrics collection
5. **Caching:** Configure Redis or similar for distributed caching
6. **Load Balancing:** Use multiple server instances behind a load balancer

## 📚 References

- [NuGet V3 API Documentation](https://docs.microsoft.com/en-us/nuget/api/overview)
- [NuGet Protocol Specification](https://github.com/NuGet/NuGetGallery/wiki/NuGet-V3-API)
- [ASP.NET Core Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
