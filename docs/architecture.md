# Architecture Documentation

## Overview

Nupack Server is built using a clean, layered architecture that promotes maintainability, testability, and scalability. The system follows Domain-Driven Design principles and implements the Repository pattern for data access.

## System Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        WebUI[Web UI]
        CLI[.NET CLI]
        API[API Clients]
    end
    
    subgraph "Presentation Layer"
        MinimalAPI[Minimal APIs]
        Swagger[Swagger/OpenAPI]
        StaticFiles[Static File Middleware]
    end
    
    subgraph "Application Layer"
        PackageService[Package Service]
        ValidationService[Validation Service]
        CacheService[Cache Service]
    end
    
    subgraph "Domain Layer"
        PackageMetadata[Package Metadata]
        ApiResponse[API Response Models]
        DTOs[Data Transfer Objects]
    end
    
    subgraph "Infrastructure Layer"
        FileStorage[File System Storage]
        MemoryCache[In-Memory Cache]
        NuGetPackaging[NuGet.Packaging]
    end
    
    WebUI --> MinimalAPI
    CLI --> MinimalAPI
    API --> MinimalAPI
    
    MinimalAPI --> PackageService
    PackageService --> FileStorage
    PackageService --> MemoryCache
    FileStorage --> NuGetPackaging
```

## Layer Responsibilities

### 1. Presentation Layer

**Location**: `src/Nupack.Server.Api/Program.cs`

**Responsibilities**:
- HTTP request/response handling
- API endpoint routing
- Input validation
- Response formatting
- Authentication/Authorization (future)

**Components**:
- **Minimal APIs**: High-performance HTTP endpoints
- **Swagger/OpenAPI**: Interactive documentation
- **Static File Middleware**: Serves web UI and package files
- **CORS Middleware**: Cross-origin request handling

### 2. Application Layer

**Location**: `src/Nupack.Server.Api/Services/`

**Responsibilities**:
- Business logic orchestration
- Service coordination
- Transaction management
- Error handling and logging

**Components**:
- **IPackageService**: Core business logic interface
- **PackageService**: Business logic implementation
- **Validation Logic**: Input and business rule validation
- **Caching Strategy**: Performance optimization

### 3. Domain Layer

**Location**: `src/Nupack.Server.Api/Models/`

**Responsibilities**:
- Domain entities and value objects
- Business rules and invariants
- Domain events (future)

**Components**:
- **PackageMetadata**: Core domain entity
- **ApiResponse<T>**: Standardized response wrapper
- **Request/Response DTOs**: Data transfer contracts

### 4. Infrastructure Layer

**Location**: `src/Nupack.Server.Api/Services/`

**Responsibilities**:
- External system integration
- Data persistence
- File system operations
- Third-party library integration

**Components**:
- **IPackageStorageService**: Storage abstraction
- **FileSystemPackageStorageService**: File system implementation
- **NuGet.Packaging Integration**: Package processing
- **In-Memory Caching**: Performance optimization

## Component Diagram

```mermaid
classDiagram
    class IPackageService {
        <<interface>>
        +UploadPackageAsync(request) ApiResponse~PackageMetadata~
        +SearchPackagesAsync(request) ApiResponse~PackageListResponse~
        +GetPackageAsync(id, version) ApiResponse~PackageMetadata~
        +DownloadPackageAsync(id, version) ApiResponse~Stream~
        +DeletePackageAsync(id, version) ApiResponse~bool~
    }
    
    class PackageService {
        -IPackageStorageService storageService
        -ILogger logger
        +UploadPackageAsync(request) ApiResponse~PackageMetadata~
        +SearchPackagesAsync(request) ApiResponse~PackageListResponse~
        +GetPackageAsync(id, version) ApiResponse~PackageMetadata~
        +DownloadPackageAsync(id, version) ApiResponse~Stream~
        +DeletePackageAsync(id, version) ApiResponse~bool~
    }
    
    class IPackageStorageService {
        <<interface>>
        +StorePackageAsync(file) PackageMetadata
        +GetPackagesAsync(query, skip, take) IEnumerable~PackageMetadata~
        +GetPackageAsync(id, version) PackageMetadata
        +GetPackageStreamAsync(id, version) Stream
        +DeletePackageAsync(id, version) bool
        +GetTotalPackageCountAsync(query) int
    }
    
    class FileSystemPackageStorageService {
        -string packagesPath
        -ConcurrentDictionary cache
        -ILogger logger
        +StorePackageAsync(file) PackageMetadata
        +GetPackagesAsync(query, skip, take) IEnumerable~PackageMetadata~
        +GetPackageAsync(id, version) PackageMetadata
        +GetPackageStreamAsync(id, version) Stream
        +DeletePackageAsync(id, version) bool
        +GetTotalPackageCountAsync(query) int
        -InitializeCache() void
    }
    
    class PackageMetadata {
        +string Id
        +string Version
        +string Title
        +string Description
        +string Authors
        +string Tags
        +DateTime Created
        +long Size
        +string FileName
    }
    
    IPackageService <|-- PackageService
    IPackageStorageService <|-- FileSystemPackageStorageService
    PackageService --> IPackageStorageService
    FileSystemPackageStorageService --> PackageMetadata
```

## Data Flow

### Package Upload Flow

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Service
    participant Storage
    participant FileSystem
    
    Client->>API: POST /api/v1/packages
    API->>API: Validate multipart form
    API->>Service: UploadPackageAsync(request)
    Service->>Service: Validate business rules
    Service->>Storage: StorePackageAsync(file)
    Storage->>Storage: Extract package metadata
    Storage->>FileSystem: Save .nupkg file
    Storage->>Storage: Update in-memory cache
    Storage-->>Service: Return PackageMetadata
    Service-->>API: Return ApiResponse
    API-->>Client: Return JSON response
```

### Package Search Flow

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Service
    participant Storage
    participant Cache
    
    Client->>API: GET /api/v1/packages?q=search
    API->>Service: SearchPackagesAsync(request)
    Service->>Storage: GetPackagesAsync(query, skip, take)
    Storage->>Cache: Query in-memory cache
    Cache-->>Storage: Return filtered results
    Storage-->>Service: Return PackageListResponse
    Service-->>API: Return ApiResponse
    API-->>Client: Return JSON response
```

## Design Patterns

### 1. Repository Pattern
- **Interface**: `IPackageStorageService`
- **Implementation**: `FileSystemPackageStorageService`
- **Benefits**: Abstraction over data access, testability, flexibility

### 2. Service Layer Pattern
- **Interface**: `IPackageService`
- **Implementation**: `PackageService`
- **Benefits**: Business logic encapsulation, transaction management

### 3. Dependency Injection
- **Container**: Built-in .NET DI container
- **Lifetime**: Scoped services for request-based operations
- **Benefits**: Loose coupling, testability, configuration flexibility

### 4. Response Wrapper Pattern
- **Type**: `ApiResponse<T>`
- **Benefits**: Consistent API responses, error handling standardization

## Performance Considerations

### Caching Strategy

```mermaid
graph LR
    Request[API Request] --> Cache{Cache Hit?}
    Cache -->|Yes| Return[Return Cached Data]
    Cache -->|No| FileSystem[Read from File System]
    FileSystem --> UpdateCache[Update Cache]
    UpdateCache --> Return
```

**Cache Implementation**:
- **Type**: `ConcurrentDictionary<string, PackageMetadata>`
- **Key**: `{packageId}:{version}`
- **Invalidation**: On package upload/delete
- **Benefits**: Sub-millisecond metadata access

### File I/O Optimization

- **Streaming**: Direct file streaming for downloads
- **Async Operations**: Non-blocking I/O operations
- **Memory Management**: Proper disposal of streams and resources

## Security Architecture

### Input Validation

```mermaid
graph TD
    Input[User Input] --> Validation{Validation}
    Validation -->|Valid| Processing[Process Request]
    Validation -->|Invalid| Error[Return Error]
    Processing --> FileValidation{File Validation}
    FileValidation -->|Valid .nupkg| Storage[Store Package]
    FileValidation -->|Invalid| Error
```

**Validation Layers**:
1. **HTTP Level**: Content-Type, file size limits
2. **Application Level**: Business rule validation
3. **File Level**: NuGet package format validation

### Error Handling

- **Structured Logging**: Comprehensive error tracking
- **Safe Error Messages**: No sensitive information exposure
- **Exception Handling**: Graceful degradation

## Scalability Considerations

### Current Limitations

- **Single Instance**: No horizontal scaling support
- **File System Storage**: Limited by disk I/O
- **In-Memory Cache**: Lost on restart

### Future Enhancements

- **Database Storage**: SQL Server, PostgreSQL support
- **Distributed Cache**: Redis integration
- **Load Balancing**: Multiple instance support
- **Cloud Storage**: Azure Blob, AWS S3 integration

## Technology Stack

### Core Technologies

- **.NET 8**: Latest LTS runtime
- **ASP.NET Core**: Web framework
- **Minimal APIs**: High-performance endpoints
- **NuGet.Packaging**: Package processing library

### Development Tools

- **Swagger/OpenAPI**: API documentation
- **Docker**: Containerization
- **xUnit**: Testing framework (future)
- **Serilog**: Structured logging (future)

## Deployment Architecture

### Development Environment

```mermaid
graph LR
    Developer[Developer] --> IDE[IDE/VS Code]
    IDE --> LocalServer[Local Server :5000]
    LocalServer --> FileSystem[Local File System]
```

### Production Environment

```mermaid
graph LR
    Client[Clients] --> LoadBalancer[Load Balancer]
    LoadBalancer --> Container1[Container Instance 1]
    LoadBalancer --> Container2[Container Instance 2]
    Container1 --> SharedStorage[Shared Storage]
    Container2 --> SharedStorage
```

## Monitoring and Observability

### Logging Strategy

- **Structured Logging**: JSON format for log aggregation
- **Log Levels**: Appropriate use of Debug, Info, Warning, Error
- **Correlation IDs**: Request tracing across components

### Metrics Collection

- **Performance Metrics**: Response times, throughput
- **Business Metrics**: Package uploads, downloads, searches
- **System Metrics**: Memory usage, disk space, CPU utilization

### Health Checks

- **Endpoint**: `/health` (future enhancement)
- **Dependencies**: File system access, memory usage
- **Integration**: Kubernetes readiness/liveness probes

## Testing Strategy

### Unit Testing

- **Service Layer**: Business logic validation
- **Storage Layer**: Data access operations
- **Model Validation**: DTO and entity testing

### Integration Testing

- **API Endpoints**: End-to-end request/response testing
- **File Operations**: Package upload/download workflows
- **Error Scenarios**: Exception handling validation

### Performance Testing

- **Load Testing**: Concurrent user simulation
- **Stress Testing**: Resource limit validation
- **Benchmark Testing**: Performance regression detection

---

This architecture provides a solid foundation for a production-ready NuGet server while maintaining flexibility for future enhancements and scaling requirements.
