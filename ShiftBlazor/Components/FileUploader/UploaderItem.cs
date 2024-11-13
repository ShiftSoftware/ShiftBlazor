using Microsoft.AspNetCore.Components.Forms;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

public class UploaderItem
{
    public Guid Id = Guid.NewGuid();
    public IBrowserFile? LocalFile { get; set; }
    public ShiftFileDTO? File { get; set; }
    public Message? Message { get; set; }
    public string? RelativePath { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public UploaderItem(IBrowserFile file, string? relativePath = null)
    {
        LocalFile = file;
        RelativePath = relativePath;
        CancellationTokenSource = new CancellationTokenSource();
    }

    public UploaderItem(ShiftFileDTO file)
    {
        File = file;
    }

    public bool IsImage()
    {
        var fileName = GetFileName();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return MimeTypes.GetMimeType(fileName).StartsWith("image/");
    }

    public string GetFileName() => (File?.Name ?? LocalFile?.Name)!;

    public bool IsNew() => File != null && LocalFile != null;
    public bool IsWaitingForUpload() => (File == null || File.Url != null) && LocalFile != null && Message == null;
}
