# Package Storage Configuration

Bu dokümantasyon, Nupack Server'da paket depolama yolunun nasıl yapılandırılacağını açıklar.

## Genel Bakış

Nupack Server, NuGet paketlerinin fiziksel olarak nereye kaydedileceğini `appsettings.json` dosyası üzerinden yapılandırmanıza olanak tanır. Bu özellik Linux, Windows ve Docker ortamlarında cross-platform uyumlu çalışır.

## Yapılandırma

### appsettings.json

```json
{
  "PackageStorage": {
    "BasePath": null
  }
}
```

### Yapılandırma Seçenekleri

#### 1. Default (Önerilen)
```json
{
  "PackageStorage": {
    "BasePath": null
  }
}
```
- `null` değeri ile varsayılan davranış korunur
- Paketler `WebRootPath/packages` veya `ContentRootPath/packages` dizinine kaydedilir

#### 2. Relative Path (Göreceli Yol)
```json
{
  "PackageStorage": {
    "BasePath": "data/packages"
  }
}
```
- Uygulama kök dizinine göre göreceli yol
- Cross-platform uyumlu (forward slash kullanın)

#### 3. Absolute Path (Mutlak Yol)
```json
{
  "PackageStorage": {
    "BasePath": "/var/nuget/packages"
  }
}
```
Linux/macOS için:
```json
{
  "PackageStorage": {
    "BasePath": "/var/nuget/packages"
  }
}
```

Windows için:
```json
{
  "PackageStorage": {
    "BasePath": "C:/NuGet/Packages"
  }
}
```

#### 4. Environment Variables (Ortam Değişkenleri)
```json
{
  "PackageStorage": {
    "BasePath": "${NUGET_PACKAGES_PATH}"
  }
}
```

Diğer örnekler:
```json
{
  "PackageStorage": {
    "BasePath": "${HOME}/nuget-packages"
  }
}
```

```json
{
  "PackageStorage": {
    "BasePath": "${APPDATA}/NuGet/Packages"
  }
}
```

## Deployment Senaryoları

### Docker Container
```json
{
  "PackageStorage": {
    "BasePath": "/app/packages"
  }
}
```

Docker Compose ile volume mount:
```yaml
services:
  nupack-server:
    image: nupack-server
    volumes:
      - ./packages:/app/packages
    environment:
      - PackageStorage__BasePath=/app/packages
```

### Kubernetes
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: nupack-config
data:
  appsettings.json: |
    {
      "PackageStorage": {
        "BasePath": "/data/packages"
      }
    }
```

### Corporate Environment
```json
{
  "PackageStorage": {
    "BasePath": "/shared/nuget/packages"
  }
}
```

### Development Environment
```json
{
  "PackageStorage": {
    "BasePath": "dev-packages"
  }
}
```

## Environment Variables ile Override

appsettings.json yerine environment variable kullanabilirsiniz:

```bash
# Linux/macOS
export PackageStorage__BasePath="/var/nuget/packages"

# Windows
set PackageStorage__BasePath=C:\NuGet\Packages

# Docker
docker run -e PackageStorage__BasePath=/app/packages nupack-server
```

## Cross-Platform Notları

1. **Path Separators**: Forward slash (/) kullanın - tüm platformlarda çalışır
2. **Environment Variables**: `${VARIABLE_NAME}` syntax'ı kullanın
3. **Permissions**: Belirtilen dizine yazma yetkisi olduğundan emin olun
4. **Volume Mounts**: Docker/Kubernetes'te volume mount'ları doğru yapılandırın

## Güvenlik Önerileri

1. **Permissions**: Sadece gerekli kullanıcılara yazma yetkisi verin
2. **Backup**: Paket dizinini düzenli olarak yedekleyin
3. **Monitoring**: Disk alanını izleyin
4. **Access Control**: Network seviyesinde erişim kontrolü uygulayın

## Troubleshooting

### Log Kontrolü
Uygulama başladığında şu log mesajını arayın:
```
Package storage path resolved to: /path/to/packages
```

### Yaygın Sorunlar

1. **Permission Denied**: Dizin yazma yetkisi kontrolü
2. **Path Not Found**: Üst dizinlerin var olduğunu kontrol edin
3. **Environment Variable Not Found**: Değişken adını kontrol edin

### Debug
Development ortamında log seviyesini artırın:
```json
{
  "Logging": {
    "LogLevel": {
      "Nupack.Server.Api.Services.FileSystemPackageStorageService": "Debug"
    }
  }
}
```
