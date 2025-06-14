# 📦 Nupack Server - Self-Hosted NuGet Package Repository

![CI](https://github.com/dgknttr/Nupack.Server/actions/workflows/ci.yml/badge.svg)
![Security](https://github.com/dgknttr/Nupack.Server/actions/workflows/security.yml/badge.svg)
![CodeQL](https://github.com/dgknttr/Nupack.Server/actions/workflows/codeql.yml/badge.svg)

A modern, **open-source NuGet v3 server implementation** with web interface, built using ASP.NET Core 9. This **private NuGet package repository** provides a powerful **NuGet.Server alternative** for hosting NuGet packages, whether for **enterprise organizations**, **development teams**, or **community package repositories**.

**🔍 Keywords**: *self-hosted NuGet server, private NuGet repository, NuGet.Server alternative, enterprise package management, Docker NuGet hosting, ASP.NET Core NuGet server*

![NuGet v3 API](https://img.shields.io/badge/NuGet-v3%20API-blue) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-purple) ![Docker](https://img.shields.io/badge/Docker-Ready-blue) ![Self-Hosted](https://img.shields.io/badge/Self--Hosted-✓-green) ![Private Repository](https://img.shields.io/badge/Private%20Repository-✓-orange) ![License](https://img.shields.io/badge/License-MIT-green) ![Enterprise Ready](https://img.shields.io/badge/Enterprise-Ready-red)

## 📸 Screenshots & Interface Preview

<div align="center">

### 🏠 **Main Dashboard - Package Overview**
*Browse and search your private NuGet packages with an intuitive web interface*

![Main Dashboard](docs/screenshots/dashboard-preview.png)

### 📦 **Package Details - Installation Commands**
*Detailed package information with copy-to-clipboard install commands*

![Package Details](docs/screenshots/package-details-preview.png)

### ⬆️ **Package Upload - Web Interface**
*Easy drag-and-drop package upload directly through the web browser*

![Package Upload](docs/screenshots/upload-interface-preview.png)

</div></div>

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

## 🚀 Getting Started - Deploy Your Private NuGet Server

Set up your own **self-hosted NuGet package repository** in minutes! This guide will help you deploy a **private NuGet server** that serves as a powerful **NuGet.Server alternative** for your organization.

### Prerequisites
- .NET 9.0 SDK (or .NET 8.0 for compatibility)
- (Optional) Docker for containerized deployment
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

### 🎯 Real-World Use Cases & Scenarios

#### 🏢 **Enterprise & Corporate Scenarios**
- **Internal Package Repository**: Host proprietary .NET libraries and shared components across development teams
- **Microservices Architecture**: Centralized package management for distributed systems and service libraries
- **CI/CD Pipeline Integration**: Automated package publishing and consumption in DevOps workflows
- **Compliance & Security**: Keep sensitive packages on-premises to meet regulatory requirements (GDPR, HIPAA, SOX)
- **Multi-Team Collaboration**: Share reusable components between different departments and projects

#### 🌐 **Community & Open Source Scenarios**
- **Open Source Project Hosting**: Alternative to nuget.org for community packages and pre-release versions
- **Regional Package Mirrors**: Faster package access for specific geographic regions or organizations
- **Educational Institutions**: Teaching package management and software distribution concepts
- **Startup & Small Teams**: Cost-effective alternative to commercial NuGet hosting services
- **Development Sandbox**: Testing and experimenting with package distribution before public release

#### 🔧 **Technical & Development Scenarios**
- **Offline Development**: Air-gapped environments where internet access is restricted
- **Custom Package Workflows**: Specialized package validation, approval, and distribution processes
- **Legacy System Support**: Hosting older package versions not available on public repositories
- **Performance Optimization**: Reduced latency and bandwidth usage for frequently accessed packages
- **Backup & Redundancy**: Secondary package repository for business continuity planning

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

## 🔍 SEO & Discovery Keywords

**Primary Keywords**: `self-hosted nuget server`, `private nuget repository`, `nuget.server alternative`, `enterprise package management`, `asp.net core nuget server`

**Secondary Keywords**: `docker nuget hosting`, `private package repository`, `nuget v3 server`, `open source nuget server`, `corporate nuget hosting`, `internal package management`, `nuget server implementation`, `package repository hosting`

**Technology Stack**: `ASP.NET Core`, `C#`, `Docker`, `NuGet v3 API`, `Tailwind CSS`, `Enterprise Ready`, `Self-Hosted`, `Open Source`

---

## 🌟 Support

If you find this project useful, please consider giving it a ⭐️ on GitHub to help others discover it!

## 🌟 Why Choose Nupack Server?

### ✅ **vs. NuGet.Server (Official)**
- ✅ Modern ASP.NET Core 9 (vs. legacy .NET Framework)
- ✅ Web interface included (vs. API-only)
- ✅ Docker support (vs. IIS-only)
- ✅ Active development (vs. minimal updates)
- ✅ Enterprise features (vs. basic functionality)

### ✅ **vs. Commercial Solutions**
- ✅ **Free & Open Source** (vs. expensive licensing)
- ✅ **Full control** (vs. vendor lock-in)
- ✅ **Customizable** (vs. fixed features)
- ✅ **Self-hosted** (vs. cloud dependency)
- ✅ **Community-driven** (vs. corporate priorities)

### ✅ **vs. Other Open Source Alternatives**
- ✅ **Lightweight & Fast** (optimized for performance)
- ✅ **Easy deployment** (Docker + simple configuration)
- ✅ **Modern UI** (responsive web interface)
- ✅ **Well documented** (comprehensive guides)
- ✅ **Active community** (regular updates & support)

---

*A modern, **self-hosted NuGet server** and **private package repository** solution for the .NET community.*

**🚀 Deploy your private NuGet server today • 🔒 Keep your packages secure • 💰 Save on hosting costs**

**🤝 Contributions welcome • 📄 MIT Licensed • 🌟 Made by the community, for the community**
