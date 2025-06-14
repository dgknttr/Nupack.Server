# 🎨 Nupack Server - Modern Web UI

A beautiful, responsive web interface for the Nupack Server V3 built with **ASP.NET Core Razor Pages** and **Tailwind CSS**.

## ✨ Features

### 🏠 **Home Page**
- **Package browsing** with responsive grid layout
- **Statistics dashboard** showing total, stable, and prerelease packages
- **Quick search** with large, prominent search bar
- **Configuration guide** for setting up NuGet clients
- **Quick action buttons** for search and upload

### 🔍 **Advanced Search**
- **Real-time search** with query parameters
- **Prerelease filtering** toggle
- **Pagination** for large result sets
- **Search result statistics** and context
- **Empty state handling** with helpful suggestions

### 📦 **Package Details**
- **Comprehensive package information** with metadata
- **Version history** with publish dates and status badges
- **Dependency visualization** by target framework
- **Installation commands** for multiple package managers
- **Release notes** and description display
- **Project links** and license information

### ⬆️ **Package Upload**
- **Drag & drop interface** for .nupkg files
- **File validation** and size checking
- **API key authentication** support
- **Upload progress** and status feedback
- **Package guidelines** and help documentation

### 🎨 **Modern UI/UX**
- **Tailwind CSS** for consistent, beautiful design
- **Responsive layout** that works on all devices
- **Custom NuGet blue** color scheme
- **Smooth animations** and hover effects
- **Toast notifications** for user feedback
- **Loading states** and error handling

## 🛠️ Technology Stack

- **ASP.NET Core 8** - Server-side framework
- **Razor Pages** - Server-side rendering
- **Tailwind CSS** - Utility-first CSS framework
- **HttpClient** - NuGet V3 API integration
- **JavaScript** - Enhanced interactivity

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Running Nupack Server (backend)

### Installation

1. **Navigate to the Web UI project**
   ```bash
   cd src/Nupack.Server.Web
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure the backend URL** (optional)
   Edit `appsettings.json`:
   ```json
   {
     "NuGetServer": {
       "BaseUrl": "http://localhost:5003"
     }
   }
   ```

4. **Run the application**
   ```bash
   dotnet run --urls "http://localhost:5002"
   ```

5. **Open in browser**
   ```
   http://localhost:5002
   ```

## 📱 Pages Overview

### **Home (`/`)**
- Package browsing and statistics
- Quick search functionality
- Configuration instructions
- Health status monitoring

### **Search (`/Search`)**
- Advanced package search
- Filter by prerelease versions
- Paginated results
- Search analytics

### **Package Details (`/packages/{id}`)**
- Complete package information
- Version selection and history
- Installation commands
- Dependencies and metadata

### **Upload (`/Upload`)**
- Package file upload
- Drag & drop interface
- Upload validation and feedback
- Guidelines and help

## 🎯 Key Features

### **✅ No Download Buttons Policy**
- **Install commands only** - no direct download links
- **Copy-to-clipboard** functionality for easy command usage
- **Multiple package managers** supported (dotnet CLI, PM Console, PackageReference)
- **Source configuration** included in all commands

### **✅ Responsive Design**
- **Mobile-first** approach with Tailwind CSS
- **Flexible grid layouts** that adapt to screen size
- **Touch-friendly** interactions for mobile devices
- **Consistent spacing** and typography

### **✅ Developer Experience**
- **Fast server-side rendering** with Razor Pages
- **Progressive enhancement** with JavaScript
- **Accessible design** with proper ARIA labels
- **Keyboard navigation** support

## 🔧 Configuration

### **Backend Integration**
The Web UI connects to the NuGet server via HTTP API calls:

```json
{
  "NuGetServer": {
    "BaseUrl": "http://localhost:5003",
    "Name": "Nupack Server"
  }
}
```

### **Tailwind CSS**
Custom configuration with NuGet brand colors:

```javascript
theme: {
  extend: {
    colors: {
      'nuget-blue': '#004880',
      'nuget-blue-light': '#0078d4',
      'nuget-blue-dark': '#003366'
    }
  }
}
```

## 🧩 Architecture

### **Modular Components**
- `_PackageCard.cshtml` - Package display cards
- `_InstallCommand.cshtml` - Installation command blocks
- `_SearchBar.cshtml` - Search interface
- `_VersionList.cshtml` - Version selection
- `_ErrorDisplay.cshtml` - Error handling

### **Services**
- `INuGetApiService` - NuGet V3 API client interface
- `NuGetApiService` - HTTP client implementation

### **Models**
- `PackageSearchResult` - Search result data
- `CatalogEntry` - Package metadata
- `RegistrationIndex` - Version information

## 🎨 UI Components

### **Package Cards**
```html
<div class="card package-card group">
  <!-- Package icon, title, version -->
  <!-- Description and metadata -->
  <!-- Install command and actions -->
</div>
```

### **Install Commands**
```html
<div class="code-block">
  <code>dotnet add package {PackageId} --version {Version} --source "Nupack Server"</code>
  <button onclick="copyCommand()">Copy</button>
</div>
```

### **Search Interface**
```html
<form asp-page="/Search" method="get">
  <input type="text" name="q" placeholder="Search packages..." />
  <input type="checkbox" name="includePrerelease" />
  <button type="submit">Search</button>
</form>
```

## 🔍 API Integration

### **Service Discovery**
```csharp
var serviceIndex = await _nugetApiService.GetServiceIndexAsync();
```

### **Package Search**
```csharp
var results = await _nugetApiService.SearchPackagesAsync(
    query: "test",
    skip: 0,
    take: 20,
    includePrerelease: true
);
```

### **Package Details**
```csharp
var registration = await _nugetApiService.GetPackageRegistrationAsync("PackageId");
```

## 🚀 Deployment

### **Development**
```bash
dotnet run --urls "http://localhost:5002"
```

### **Production**
```bash
dotnet publish -c Release
# Deploy published files to web server
```

### **Docker** (Optional)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY published/ /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "Nupack.Server.Web.dll"]
```

## 📊 Performance

- **Server-side rendering** for fast initial page loads
- **Minimal JavaScript** for enhanced functionality
- **Efficient HTTP client** with connection pooling
- **Responsive images** and optimized assets

## 🔒 Security

- **Input validation** on all forms
- **XSS protection** through Razor encoding
- **CSRF protection** with anti-forgery tokens
- **Secure HTTP client** configuration

## 🎯 Browser Support

- **Modern browsers** (Chrome, Firefox, Safari, Edge)
- **Mobile browsers** with responsive design
- **Progressive enhancement** for older browsers
- **Accessibility** compliance (WCAG 2.1)

---

## 🎉 **SUCCESS! Web UI is Complete and Running!**

The Nupack Server now has a beautiful, modern web interface that provides:

✅ **Complete package management** functionality  
✅ **Responsive design** for all devices  
✅ **Developer-friendly** install commands  
✅ **Modern UI/UX** with Tailwind CSS  
✅ **Server-side rendering** for performance  

**🌐 Access the Web UI at: http://localhost:5002**
