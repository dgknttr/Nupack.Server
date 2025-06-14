# Contributing to Nupack Server

We love your input! We want to make contributing to Nupack Server as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## Development Process

We use GitHub to host code, to track issues and feature requests, as well as accept pull requests.

## Pull Requests

Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. **Fork the repo** and create your branch from `main`.
2. **Use proper branch naming**:
   - `feature/your-feature-name` for new features
   - `bugfix/issue-description` for bug fixes
   - `docs/documentation-update` for documentation changes
3. **Add tests** if you've added code that should be tested.
4. **Update documentation** if you've changed APIs.
5. **Ensure the test suite passes** by running `dotnet test`.
6. **Format your code** using `dotnet format` for C# and Prettier for CSS/JS.
7. **Make sure your code follows** the existing code style.
8. **Issue that pull request!**

## Development Setup

### Prerequisites

- .NET 8.0 SDK or higher
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

### Local Development

1. **Clone your fork**:
   ```bash
   git clone https://github.com/your-username/nupack-server.git
   cd nupack-server
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run tests**:
   ```bash
   dotnet test
   ```

5. **Start the development servers**:
   ```bash
   # Terminal 1: Start the NuGet API server
   cd src/Nupack.Server.Api
   dotnet run --urls "http://localhost:5003"

   # Terminal 2: Start the Web UI
   cd src/Nupack.Server.Web
   dotnet run --urls "http://localhost:5002"
   ```

### Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and small
- Use async/await for I/O operations

#### Code Formatting

Before submitting a pull request, ensure your code is properly formatted:

```bash
# Format C# code
dotnet format

# Format CSS/JavaScript (if you have Node.js installed)
cd src/Nupack.Server.Web
npm run format
```

### Testing

- Write unit tests for new functionality
- Ensure all tests pass before submitting PR
- Aim for good test coverage
- Use descriptive test names

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Bug Reports

We use GitHub issues to track public bugs. Report a bug by opening a new issue in the repository.

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

### Bug Report Template

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Environment:**
 - OS: [e.g. Windows 11, Ubuntu 20.04]
 - .NET Version: [e.g. 8.0.1]
 - Browser: [e.g. Chrome 120, Firefox 121]

**Additional context**
Add any other context about the problem here.
```

## Feature Requests

We welcome feature requests! Please provide:

- **Use case**: Describe the problem you're trying to solve
- **Proposed solution**: How you envision the feature working
- **Alternatives considered**: Other approaches you've thought about
- **Additional context**: Screenshots, mockups, etc.

## Security Issues

Please do not report security vulnerabilities through public GitHub issues. Instead, please refer to our [Security Policy](SECURITY.md).

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

Feel free to open an issue with the `question` label, or reach out to the maintainers directly.

## Recognition

Contributors will be recognized in our README and release notes. Thank you for making Nupack Server better! 🎉
