namespace Nupack.Server.Storage;

public sealed class PackageUploadOptions
{
    public const string SectionName = "PackageUpload";
    public const long DefaultMaxPackageSizeBytes = 100L * 1024 * 1024;

    private const long MultipartEnvelopeAllowanceBytes = 1024 * 1024;

    public long MaxPackageSizeBytes { get; set; } = DefaultMaxPackageSizeBytes;

    public long GetResolvedMaxPackageSizeBytes()
        => MaxPackageSizeBytes > 0 ? MaxPackageSizeBytes : DefaultMaxPackageSizeBytes;

    public long GetRequestBodySizeLimitBytes()
        => GetResolvedMaxPackageSizeBytes() + MultipartEnvelopeAllowanceBytes;

    public string GetMaxPackageSizeDisplay()
    {
        var bytes = GetResolvedMaxPackageSizeBytes();
        if (bytes % (1024 * 1024) == 0)
        {
            return $"{bytes / (1024 * 1024)}MB";
        }

        if (bytes % 1024 == 0)
        {
            return $"{bytes / 1024}KB";
        }

        return $"{bytes} bytes";
    }
}
