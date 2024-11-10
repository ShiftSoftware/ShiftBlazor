using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using Syncfusion.Blazor.FileManager;
using Syncfusion.Blazor.Navigations;
using Blazored.LocalStorage;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileExplorer
{
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] ISyncLocalStorageService SyncLocalStorage { get; set; } = default!;

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? Root { get; set; }

    [Parameter]
    public double MaxUploadSizeInBytes { get; set; } = 128;

    [Parameter]
    public ViewType View { get; set; } = ViewType.LargeIcons;

    [Parameter]
    public string? AccountName { get; set; }

    [Parameter]
    public string? ContainerName { get; set; }

    [Parameter]
    public string Height { get; set; } = "600px";

    [Parameter]
    public string? RootAliasName { get; set; }

    public List<ToolBarItemModel> Items = new();

    private SfFileManager<FileManagerDirectoryContent>? SfFileManager { get; set; }
    private string? Url;
    private double MaxUploadSize => MaxUploadSizeInBytes * 1024 * 1024;
    private FileUploader? _FileUploader { get; set; }
    private string FileManagerId { get; set; }

    protected override void OnInitialized()
    {
        FileManagerId = "FileExplorer" + Guid.NewGuid().ToString().Replace("-", string.Empty);

        var url = BaseUrl
            ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "")
            ?? SettingManager.Configuration.ApiPath;

        Url = url.AddUrlPath("FileExplorer");

        this.HttpClient.DefaultRequestHeaders.Add("Root-Dir", Root);

        Items = new List<ToolBarItemModel>(){
            new ToolBarItemModel() { Name = "NewFolder" },
            //new ToolBarItemModel() { Name = "Cut" },
            //new ToolBarItemModel() { Name = "Copy" },
            //new ToolBarItemModel() { Name = "Paste" },
            new ToolBarItemModel() { Name = "Upload"},
            new ToolBarItemModel() { Name = "DirectAzureUpload", Text="Upload Files", TooltipText="Upload Files", PrefixIcon="e-icons e-import", Visible=true, Click=new EventCallback<ClickEventArgs>(null, UploadFiles)},
            //new ToolBarItemModel() { Name = "DirectAzureDirUpload", Text="Upload Folders", TooltipText="Upload Folders", PrefixIcon="e-icons e-import", Visible=true, Click=new EventCallback<ClickEventArgs>(null, UploadDir)},
            new ToolBarItemModel() { Name = "SortBy" },
            new ToolBarItemModel() { Name = "Refresh" },
            new ToolBarItemModel() { Name = "Delete" },
            new ToolBarItemModel() { Name = "Download" },
            //new ToolBarItemModel() { Name = "Rename" },
            new ToolBarItemModel() { Name = "Selection" },
            new ToolBarItemModel() { Name = "View" },
            new ToolBarItemModel() { Name = "Details" },
            //new ToolBarItemModel() { Name = "Zip", Text="Zip", TooltipText="Zip Files", PrefixIcon="e-icons e-import", Visible=false, Click=new EventCallback<ClickEventArgs>(null, ZipFiles)},
            //new ToolBarItemModel() { Name = "Unzip", Text="Unzip", TooltipText="Unzip Files", PrefixIcon="e-icons e-export", Visible=false, Click=new EventCallback<ClickEventArgs>(null, UnzipFiles)},
        };
    }

    public void OnFileSelected(FileSelectEventArgs<FileManagerDirectoryContent> args)
    {
        if (SfFileManager == null) return;

        SfFileManager.PreventRender();

        var isMoreThanOneFileSelected = SfFileManager.SelectedItems.Length > 1;
        var isOneFileSelected = SfFileManager.SelectedItems.Length == 1;
        var isDirSelected = SfFileManager.GetSelectedFiles().Any(x => !x.IsFile);
        var isZipFile = SfFileManager.GetSelectedFiles().Any(x => x.IsFile && x.Name.EndsWith(".zip"));

        //Items.First(x => x.Name.Equals("Zip")).Visible = isMoreThanOneFileSelected || isOneFileSelected && isDirSelected;
        //Items.First(x => x.Name.Equals("Unzip")).Visible = isOneFileSelected && isZipFile;
        //Items.First(x => x.Name.Equals("DirectAzureUpload")).Visible = SfFileManager.SelectedItems.Length == 0;


        SfFileManager.PreventRender(false);
    }

    //private void ZipFiles(ClickEventArgs args)
    //{
    //    if (SfFileManager == null) return;

    //    var fileNames = SfFileManager.GetSelectedFiles().Select(x => x.IsFile ? x.Name : x.Name + "/");

    //    var filesToZip = new ZipOptionsDTO
    //    {
    //        ContainerName = "development",
    //        Path = FileManagerRoot + SfFileManager.Path,
    //        Names = fileNames,
    //    };
    //    HttpClient.PostAsJsonAsync(Url.AddUrlPath("ZipFiles"), filesToZip);
    //}

    //private void UnzipFiles(ClickEventArgs args)
    //{
    //    if (SfFileManager == null) return;

    //    var zipFileInfo = new ZipOptionsDTO
    //    {
    //        ContainerName = "development",
    //        Path = FileManagerRoot + SfFileManager.Path,
    //        Names = [SfFileManager.SelectedItems.First()],
    //    };
    //    HttpClient.PostAsJsonAsync(Url.AddUrlPath("UnzipFiles"), zipFileInfo);
    //}

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
            await _FileUploader.OpenInput(directoryUpload: false);
        }
    }

    //private async Task UploadDir()
    //{
    //    if (_FileUploader != null)
    //    {
    //        await _FileUploader.OpenInput(directoryUpload: true);
    //    }
    //}

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

    private void OnSearching(SearchEventArgs<FileManagerDirectoryContent> args)
    {
        args.Cancel = true;
    }

    private async Task OnRead(ReadEventArgs<FileManagerDirectoryContent> args)
    {
        if (SfFileManager == null) return;
        SyncLocalStorage.RemoveItem(SfFileManager.ID);
    }
}
