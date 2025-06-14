# Testing Guide

This directory contains the test suite for Nupack Server, including unit tests, integration tests, and testing utilities.

## Test Structure

```
tests/
├── Nupack.Server.Tests/
│   ├── Services/                 # Unit tests for service layer
│   │   └── PackageServiceTests.cs
│   ├── Integration/              # Integration tests
│   │   └── ApiIntegrationTests.cs
│   └── Nupack.Server.Tests.csproj
└── README.md                     # This file
```

## Test Categories

### Unit Tests

**Location**: `tests/Nupack.Server.Tests/Services/`

Unit tests focus on testing individual components in isolation using mocking frameworks.

**Coverage**:
- Service layer business logic
- Model validation
- Error handling scenarios
- Edge cases and boundary conditions

**Example**:
```csharp
[Fact]
public async Task SearchPackagesAsync_WithValidRequest_ReturnsSuccessResponse()
{
    // Arrange
    var request = new PackageSearchRequest("test", 0, 10);
    
    // Act
    var result = await _packageService.SearchPackagesAsync(request);
    
    // Assert
    result.Success.Should().BeTrue();
}
```

### Integration Tests

**Location**: `tests/Nupack.Server.Tests/Integration/`

Integration tests verify the complete request/response flow through the API endpoints.

**Coverage**:
- HTTP endpoint functionality
- Request/response serialization
- Error handling and status codes
- End-to-end workflows

**Example**:
```csharp
[Fact]
public async Task GetServiceIndex_ReturnsSuccessResponse()
{
    // Act
    var response = await _client.GetAsync("/v3/index.json");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Running Tests

### Prerequisites

- .NET 8.0 SDK
- Test packages are automatically restored with `dotnet restore`

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Nupack.Server.Tests/

# Run specific test class
dotnet test --filter "ClassName=PackageServiceTests"

# Run specific test method
dotnet test --filter "MethodName=SearchPackagesAsync_WithValidRequest_ReturnsSuccessResponse"
```

### Visual Studio

1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test → Test Explorer)
4. Click "Run All Tests" or run individual tests

### VS Code

1. Install the .NET Test Explorer extension
2. Open the command palette (Ctrl+Shift+P)
3. Run ".NET: Test Explorer: Run All Tests"

## Test Configuration

### Test Project Configuration

The test project includes the following packages:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.69" />
```

### Test Environment

Integration tests use the `WebApplicationFactory<Program>` to create an in-memory test server:

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
}
```

## Writing Tests

### Unit Test Guidelines

1. **Arrange-Act-Assert Pattern**: Structure tests clearly
2. **Descriptive Names**: Use descriptive test method names
3. **Single Responsibility**: Test one thing per test method
4. **Mock Dependencies**: Use Moq for external dependencies
5. **Fluent Assertions**: Use FluentAssertions for readable assertions

**Example**:
```csharp
[Fact]
public async Task GetPackageAsync_WithNonExistentPackage_ReturnsNotFoundResponse()
{
    // Arrange
    var packageId = "NonExistentPackage";
    var version = "1.0.0";
    _mockStorageService.Setup(x => x.GetPackageAsync(packageId, version))
        .ReturnsAsync((PackageMetadata?)null);

    // Act
    var result = await _packageService.GetPackageAsync(packageId, version);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Package not found");
}
```

### Integration Test Guidelines

1. **Test Real Scenarios**: Test actual HTTP requests/responses
2. **Verify Status Codes**: Check appropriate HTTP status codes
3. **Validate Content**: Verify response content and headers
4. **Test Error Cases**: Include negative test scenarios
5. **Use Theory Tests**: Use `[Theory]` for parameterized tests

**Example**:
```csharp
[Theory]
[InlineData("/api/v1/packages")]
[InlineData("/packages")]
public async Task Endpoints_ReturnSuccessAndCorrectContentType(string url)
{
    // Act
    var response = await _client.GetAsync(url);

    // Assert
    response.EnsureSuccessStatusCode();
    response.Content.Headers.ContentType.Should().NotBeNull();
}
```

## Test Data Management

### Test Packages

For tests requiring actual NuGet packages, create minimal test packages:

```csharp
// Helper method to create test package
private static IFormFile CreateTestPackage(string id, string version)
{
    // Implementation to create a minimal .nupkg file for testing
}
```

### Test Isolation

- Each test should be independent
- Use fresh instances for each test
- Clean up resources after tests
- Use in-memory storage for integration tests

## Coverage Reports

### Generating Coverage Reports

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install report generator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Coverage Targets

- **Minimum Coverage**: 80% line coverage
- **Service Layer**: 90%+ coverage
- **Critical Paths**: 100% coverage for upload/download flows
- **Error Handling**: All exception paths covered

## Continuous Integration

### GitHub Actions

```yaml
- name: Run tests
  run: dotnet test --configuration Release --no-build --verbosity normal

- name: Generate coverage report
  run: dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"
```

### Test Results

CI/CD pipelines should:
- Run all tests on every pull request
- Generate and publish coverage reports
- Fail builds if tests fail
- Report test results in PR comments

## Performance Testing

### Load Testing

For performance testing, consider using:

```bash
# Install NBomber for load testing
dotnet add package NBomber

# Example load test
[Fact]
public void LoadTest_GetPackages_HandlesExpectedLoad()
{
    var scenario = Scenario.Create("get_packages", async context =>
    {
        var response = await httpClient.GetAsync("/api/v1/packages");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithLoadSimulations(
        Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5))
    );

    NBomberRunner
        .RegisterScenarios(scenario)
        .Run();
}
```

## Debugging Tests

### Common Issues

1. **Test Isolation**: Ensure tests don't depend on each other
2. **Async/Await**: Properly handle async operations
3. **Mock Setup**: Verify mock configurations are correct
4. **Resource Cleanup**: Dispose of resources properly

### Debugging Tips

```csharp
// Add debug output
[Fact]
public async Task DebugTest()
{
    var result = await _service.GetPackageAsync("test", "1.0.0");
    
    // Use output helper for debugging
    _output.WriteLine($"Result: {JsonSerializer.Serialize(result)}");
    
    result.Should().NotBeNull();
}
```

## Best Practices

1. **Test Naming**: Use descriptive names that explain the scenario
2. **Test Organization**: Group related tests in the same class
3. **Setup/Teardown**: Use constructors and IDisposable for setup/cleanup
4. **Assertions**: Use FluentAssertions for readable test assertions
5. **Documentation**: Comment complex test scenarios
6. **Maintenance**: Keep tests up-to-date with code changes

## Contributing

When adding new features:

1. Write tests first (TDD approach)
2. Ensure all existing tests pass
3. Add tests for new functionality
4. Update this documentation if needed
5. Verify coverage meets requirements

For more information on testing best practices, see the [Contributing Guide](../CONTRIBUTING.md).
