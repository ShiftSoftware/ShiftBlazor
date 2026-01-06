using Microsoft.AspNetCore.Components.Forms;

namespace ShiftSoftware.ShiftBlazor.Utils;

public class TransformedBrowserFile(byte[] content, string name, string contentType, DateTimeOffset lastModified)
    : IBrowserFile
{
    public string Name => name;
    public DateTimeOffset LastModified => lastModified;
    public long Size => content.Length;
    public string ContentType => contentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000L, CancellationToken cancellationToken = default)
    {
        if (Size > maxAllowedSize)
            throw new IOException($"File size exceeds maximum allowed size of {maxAllowedSize} bytes.");

        return new MemoryStream(content);
    }
}