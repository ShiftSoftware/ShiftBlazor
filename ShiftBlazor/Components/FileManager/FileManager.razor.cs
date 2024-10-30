using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Syncfusion.Blazor.FileManager;
using Syncfusion.Blazor.Navigations;
using System.Net.Http.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileManager
{
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;


    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public double MaxUploadSizeInBytes { get; set; } = 128;

    [Parameter]
    public ViewType View { get; set; } = ViewType.LargeIcons;

    [Parameter]
    public string? AccountName { get; set; }

    [Parameter]
    public string? ContainerName { get; set; }

    public List<ToolBarItemModel> Items = new();

    private SfFileManager<FileManagerDirectoryContent>? SfFileManager { get; set; }
    private string? Url;
    private double MaxUploadSize => MaxUploadSizeInBytes * 1024 * 1024;
    private FileUploader? _FileUploader { get; set; }
    private string FileManagerId = "FileManager" + Guid.NewGuid().ToString().Replace("-", string.Empty);

    protected override void OnInitialized()
    {
        var url = BaseUrl
            ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "")
            ?? SettingManager.Configuration.ApiPath;

        Url = url.AddUrlPath("FileManager");

        Items = new List<ToolBarItemModel>(){
            new ToolBarItemModel() { Name = "NewFolder" },
            new ToolBarItemModel() { Name = "Cut" },
            new ToolBarItemModel() { Name = "Copy" },
            new ToolBarItemModel() { Name = "Paste" },
            new ToolBarItemModel() { Name = "Upload"},
            new ToolBarItemModel() { Name = "DirectAzureUpload", Text="Upload Files", TooltipText="Upload Files", PrefixIcon="e-icons e-import", Visible=true, Click=new EventCallback<ClickEventArgs>(null, UploadFiles)},
            new ToolBarItemModel() { Name = "SortBy" },
            new ToolBarItemModel() { Name = "Refresh" },
            new ToolBarItemModel() { Name = "Delete" },
            new ToolBarItemModel() { Name = "Download" },
            new ToolBarItemModel() { Name = "Rename" },
            new ToolBarItemModel() { Name = "Selection" },
            new ToolBarItemModel() { Name = "View" },
            new ToolBarItemModel() { Name = "Details" },
            new ToolBarItemModel() { Name = "Zip", Text="Zip", TooltipText="Zip Files", PrefixIcon="e-icons e-import", Visible=false, Click=new EventCallback<ClickEventArgs>(null, ZipFiles)},
            new ToolBarItemModel() { Name = "Unzip", Text="Unzip", TooltipText="Unzip Files", PrefixIcon="e-icons e-export", Visible=false, Click=new EventCallback<ClickEventArgs>(null, UnzipFiles)},
        };
    }

    public void OnFileSelected(FileSelectEventArgs<FileManagerDirectoryContent> args)
    {
        if (SfFileManager == null) return;

        var isMoreThanOneFileSelected = SfFileManager.SelectedItems.Length > 1;
        var isOneFileSelected = SfFileManager.SelectedItems.Length == 1;
        var isDirSelected = SfFileManager.GetSelectedFiles().Any(x => !x.IsFile);
        var isZipFile = SfFileManager.GetSelectedFiles().Any(x => x.IsFile && x.Name.EndsWith(".zip"));

        Items.First(x => x.Name.Equals("Zip")).Visible = isMoreThanOneFileSelected || isOneFileSelected && isDirSelected;
        Items.First(x => x.Name.Equals("Unzip")).Visible = isOneFileSelected && isZipFile;
        Items.First(x => x.Name.Equals("DirectAzureUpload")).Visible = SfFileManager.SelectedItems.Length == 0;
    }

    private void ZipFiles(ClickEventArgs args)
    {
        if (SfFileManager == null) return;

        var fileNames = SfFileManager.GetSelectedFiles().Select(x => x.IsFile ? x.Name : x.Name + "/");

        var filesToZip = new ZipOptionsDTO
        {
            ContainerName = "development",
            Path = "FileManager" + SfFileManager.Path,
            Names = fileNames,
        };
        HttpClient.PostAsJsonAsync(Url.AddUrlPath("ZipFiles"), filesToZip);
    }

    private void UnzipFiles(ClickEventArgs args)
    {
        if (SfFileManager == null) return;

        var zipFileInfo = new ZipOptionsDTO
        {
            ContainerName = "development",
            Path = "FileManager" + SfFileManager.Path,
            Names = [SfFileManager.SelectedItems.First()],
        };
        HttpClient.PostAsJsonAsync(Url.AddUrlPath("UnzipFiles"), zipFileInfo);
    }

    public void OnBeforeImageLoad(BeforeImageLoadEventArgs<FileManagerDirectoryContent> args)
    {
        try
        {
            args.ImageUrl = args.FileDetails.TargetPath;
        }
        catch { }
    }

    public async Task OnBeforeDownload(BeforeDownloadEventArgs<FileManagerDirectoryContent> args)
    {
        args.Cancel = true;

        foreach (var file in args.DownloadData.DownloadFileDetails)
        {
            await JsRuntime.InvokeVoidAsync("downloadFileFromUrl", file.Name, file.TargetPath);
        }
    }

    private async Task UploadFiles()
    {
        if (_FileUploader != null)
        {
            await _FileUploader.OpenInput();
        }
    }

    private void Refresh()
    {
        SfFileManager?.RefreshFilesAsync();
    }

    private void OnItemsUploading(ItemsUploadEventArgs<FileManagerDirectoryContent> args)
    {
        args.Cancel = true;
    }
    private void OnBeforePopupOpen(BeforePopupOpenCloseEventArgs args)
    {
        if (args.PopupName == "Upload")
        {
            args.Cancel = true;
        }
    }
}
