namespace Nupack.Server.Storage.Models;

public sealed class PackageUploadContent
{
    private readonly Func<Stream> _openReadStream;

    public PackageUploadContent(string fileName, long length, Func<Stream> openReadStream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Package file name is required.", nameof(fileName));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Package length cannot be negative.");
        }

        _openReadStream = openReadStream ?? throw new ArgumentNullException(nameof(openReadStream));
        FileName = fileName;
        Length = length;
    }

    public string FileName { get; }

    public long Length { get; }

    public Stream OpenReadStream() => _openReadStream();
}
