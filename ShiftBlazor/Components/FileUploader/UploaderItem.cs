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
    public double Progress { get; set; }
    public FileUploadState State { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public UploaderItem(IBrowserFile file, CancellationToken token, string? relativePath = null)
    {
        LocalFile = file;
        RelativePath = relativePath;
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        State = FileUploadState.Waiting;
    }

    public UploaderItem(ShiftFileDTO file)
    {
        File = file;
    }

    public bool IsImage() => MimeTypeStartsWith("image/");

    public bool IsVideo() => MimeTypeStartsWith("video/");

    /// <summary>An uploaded image or video, which the gallery can show; clicking it opens the gallery there.</summary>
    public bool CanPreview() => (IsImage() || IsVideo()) && State == FileUploadState.Uploaded && File?.Url != null;

    private bool MimeTypeStartsWith(string prefix)
    {
        var fileName = GetFileName();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return MimeTypes.GetMimeType(fileName).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    public string GetFileName() => (File?.Name ?? LocalFile?.Name)!;
    public bool IsWaitingForUpload => State == FileUploadState.Waiting || State == FileUploadState.Prepared;
}

public enum FileUploadState
{
    None,
    Waiting,
    Prepared,
    Uploading,
    Uploaded,
    Failed,
    Prevented,
}