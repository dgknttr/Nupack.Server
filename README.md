# 📦 Nupack Server

An open-source NuGet V3 server implementation with a web interface, built using ASP.NET Core 8. This community project provides a straightforward solution for hosting NuGet packages, whether for private organizations or community package repositories.

![Nupack Server](https://img.shields.io/badge/NuGet-V3%20API-blue) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple) ![Tailwind CSS](https://img.shields.io/badge/Tailwind%20CSS-3.0-cyan) ![License](https://img.shields.io/badge/License-MIT-green) ![Open Source](https://img.shields.io/badge/Open%20Source-Community-brightgreen)

## 📋 Features

### Core Functionality
- **NuGet V3 API** - Basic compatibility with Visual Studio, dotnet CLI, and nuget.exe
- **Package Management** - Upload and manage package versions
- **Search** - Simple package search functionality
- **Package Details** - View package information and install commands

### Web Interface
- **Responsive Design** - Works on desktop and mobile devices
- **Package Browsing** - Browse available packages with basic filtering
- **Install Commands** - Copy-to-clipboard functionality for package installation
- **Health Status** - Basic server health monitoring

### Configuration & Deployment
- **Customizable Branding** - Configure for any organization or community
- **Docker Support** - Easy containerized deployment for any environment
- **Environment Configuration** - Flexible configuration via appsettings.json or environment variables
- **Open Source** - MIT licensed for free use, modification, and distribution

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- (Optional) Node.js for CSS development

### 1. Clone and Setup
```bash
git clone https://github.com/dgknttr/nupack-server.git
cd nupack-server
```

> **Note**: This is an open-source project. Feel free to fork, modify, and adapt it to your needs!

### 2. Configuration
Copy the example configuration:
```bash
cp src/Nupack.Server.Web/appsettings.example.json src/Nupack.Server.Web/appsettings.json
```

Update `appsettings.json` with your deployment details:
```json
{
  "Branding": {
    "CompanyName": "Your Company Name",
    "NugetSourceUrl": "https://nuget.yourdomain.com/v3/index.json"
  },
  "PackageStorage": {
    "BasePath": null
  }
}
```

#### Package Storage Configuration
You can configure where packages are physically stored:

- **Default** (`null`): Uses `packages` folder in WebRootPath or ContentRootPath
- **Relative path**: `"data/packages"` - relative to application root
- **Absolute path**: `"/var/nuget/packages"` or `"C:/NuGet/Packages"`
- **Environment variables**: `"${NUGET_PACKAGES_PATH}"` or `"${HOME}/nuget-packages"`

See [Package Storage Configuration](docs/package-storage-configuration.md) for detailed examples and deployment scenarios.

> **Note**: Only `CompanyName` and `NugetSourceUrl` are configurable. All other branding elements are fixed to maintain consistent "Nupack Server" identity.

### 3. Run the Application
```bash
# Start both API and Web UI
dotnet run --project src/Nupack.Server.Api --urls "http://localhost:5003" &
dotnet run --project src/Nupack.Server.Web --urls "http://localhost:5004"
```

### 4. Access the Server
- **Web Interface**: http://localhost:5004
- **NuGet API Endpoint**: http://localhost:5003/v3/index.json

## 🌍 Open Source Benefits

This project is designed to serve the broader .NET and NuGet community by providing:

### For Developers
- **Learning Resource** - Study a real-world NuGet server implementation
- **Customization Base** - Fork and modify for specific needs
- **No Vendor Lock-in** - Full control over your package hosting

### For Organizations
- **Cost-Effective** - Free alternative to commercial NuGet hosting
- **Privacy Control** - Keep sensitive packages on your own infrastructure
- **Compliance** - Meet specific security and regulatory requirements

### For the Community
- **Collaborative Development** - Community-driven improvements and features
- **Knowledge Sharing** - Learn from and contribute to open-source practices
- **Ecosystem Growth** - Strengthen the .NET package management ecosystem

## 📦 Using the Server

### Publishing Packages
```bash
# Upload packages using dotnet CLI
dotnet nuget push package.nupkg --source http://localhost:5003/v3/index.json

# Or use the web interface upload page
# Navigate to http://localhost:5004/Upload
```

### Installing Packages
```bash
# Add the server as a package source
dotnet nuget add source "https://your-nuget-server.com/v3/index.json" --name "Nupack Server"

# Install packages from your server
dotnet add package YourPackage --source "Nupack Server"
```

## ⚙️ Configuration

### Branding Options
Only two values are configurable through `appsettings.json`:

```json
{
  "Branding": {
    "CompanyName": "Your Organization",
    "NugetSourceUrl": "https://nuget.yourdomain.com/v3/index.json"
  }
}
```

All other branding elements (ProductName, RepositoryTitle, etc.) are fixed to maintain consistent "Nupack Server" identity across all deployments.

### Available Pages
- **Home** (`/`) - Package overview and statistics
- **Search** (`/Search`) - Search and filter packages
- **Package Details** (`/Packages/Details/{id}`) - Detailed package information
- **Upload** (`/Upload`) - Upload new packages via web interface

### Community Use Cases
This open-source server can be used for:
- **Private Organizations** - Internal package repositories
- **Open Source Projects** - Community package hosting
- **Development Teams** - Shared package libraries
- **Learning & Education** - Understanding NuGet server implementation

## 🐳 Docker Deployment

The project includes a Docker setup for easy deployment:

```bash
# Build and run with Docker Compose
docker-compose up -d
```

This will start both the API server and web interface with the following ports:
- **API Server**: http://localhost:5003 (HTTP), https://localhost:5001 (HTTPS)
- **Web Interface**: http://localhost:5004 (HTTP), https://localhost:5002 (HTTPS)

You can customize the configuration by editing the environment variables in `docker-compose.yml`.

## 🔧 Additional Configuration

### Environment Variables
Configuration values can be overridden using environment variables:

```bash
# Example environment variables
Branding__ProductName="Custom NuGet Server"
Branding__CompanyName="Custom Company"
NuGetServer__BaseUrl="https://your-domain.com"
```

### Health Monitoring
- **Web UI Health**: `http://localhost:5004/health`
- **API Health**: `http://localhost:5003/health`

Both endpoints return JSON with basic server status information.

## 🤝 Contributing

We welcome contributions from the community! This is an open-source project and we appreciate any help to make it better.

### Ways to Contribute
- 🐛 **Report Issues** - Found a bug? Let us know!
- 💡 **Suggest Features** - Have an idea? We'd love to hear it
- 📝 **Improve Documentation** - Help make the docs clearer
- 🔧 **Submit Code** - Fix bugs or add new features
- 🧪 **Add Tests** - Help improve code coverage

### Getting Started with Development
1. **Fork** the repository on GitHub
2. **Clone** your fork locally
3. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
4. **Make** your changes
5. **Test** your changes thoroughly
6. **Commit** your changes (`git commit -m 'Add amazing feature'`)
7. **Push** to your branch (`git push origin feature/amazing-feature`)
8. **Open** a Pull Request

### Development Setup

#### Project Structure
```
src/
├── Nupack.Server.Api/          # NuGet V3 API implementation
├── Nupack.Server.Web/          # Web interface
└── tests/                      # Unit tests
```

#### Building from Source
```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

### Community Guidelines
- Be respectful and inclusive
- Follow existing code style and conventions
- Write clear commit messages
- Add tests for new features when possible
- Update documentation as needed

## 📄 License

This project is licensed under the **MIT License**, which means:
- ✅ **Free to use** - Use it anywhere, for any purpose
- ✅ **Free to modify** - Adapt it to your specific needs
- ✅ **Free to distribute** - Share your modifications with others
- ✅ **Commercial use allowed** - Use it in commercial projects

See the [LICENSE](LICENSE) file for the complete license text.

## 🌟 Community & Support

### Getting Help
- 📖 **Documentation** - Check this README and code comments
- 🐛 **Issues** - Report bugs or request features on GitHub Issues
- 💬 **Discussions** - Join community discussions for questions and ideas
- 📧 **Community** - Connect with other users and contributors

### Acknowledgments
- Built with [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) - Microsoft's open-source web framework
- UI styled with [Tailwind CSS](https://tailwindcss.com/) - A utility-first CSS framework
- Inspired by the official [nuget.org](https://www.nuget.org/) interface
- Thanks to all contributors who help improve this project

---

*An open-source NuGet server implementation for the .NET community.*

**🤝 Contributions welcome • 📄 MIT Licensed • 🌟 Made by the community, for the community**
