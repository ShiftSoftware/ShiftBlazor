﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using Syncfusion.Blazor.FileManager;
using Syncfusion.Blazor.Navigations;
using Blazored.LocalStorage;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileExplorer
{
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] ISyncLocalStorageService SyncLocalStorage { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? Root { get; set; }

    [Parameter]
    public string? CurrentPath { get; set; }

    [Parameter]
    public int MaxFileSizeInMegaBytes { get; set; } = 32;

    [Parameter]
    public int MaxUploadFileCount { get; set; } = 16;

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

    private SfFileManager<FileExplorerDirectoryContent>? SfFileManager { get; set; }
    private string? Url;
    private FileUploader? _FileUploader { get; set; }
    private string FileManagerId { get; set; }
    private bool ShowDeleted { get; set; } = false;
    private string DeletedItemsCss = "";

    public override Task SetParametersAsync(ParameterView parameters)
    {
        var newRoot = parameters.GetValueOrDefault<string>(nameof(Root));
        var newCurrentPath = parameters.GetValueOrDefault<string>(nameof(CurrentPath));
        if ((!string.IsNullOrWhiteSpace(Root) && Root != newRoot) || (!string.IsNullOrWhiteSpace(CurrentPath) && CurrentPath != newCurrentPath))
        {
            _ = Refresh();
            //NavigationManager.Refresh();
        }

        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        FileManagerId = "FileExplorer" + Guid.NewGuid().ToString().Replace("-", string.Empty);

        var url = BaseUrl
            ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "")
            ?? SettingManager.Configuration.ApiPath;

        Url = url.AddUrlPath("FileExplorer");

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
            new ToolBarItemModel() { Name = "ViewDeleted", TooltipText="Show Deleted Items", PrefixIcon="e-icons e-trash", Align=ItemAlign.Right, Visible=true, Click=new EventCallback<ClickEventArgs>(null, ViewDeleted)},
            new ToolBarItemModel() { Name = "View" },
            new ToolBarItemModel() { Name = "Details" },
            //new ToolBarItemModel() { Name = "Zip", Text="Zip", TooltipText="Zip Files", PrefixIcon="e-icons e-import", Visible=false, Click=new EventCallback<ClickEventArgs>(null, ZipFiles)},
            //new ToolBarItemModel() { Name = "Unzip", Text="Unzip", TooltipText="Unzip Files", PrefixIcon="e-icons e-export", Visible=false, Click=new EventCallback<ClickEventArgs>(null, UnzipFiles)},
        };
    }

    public void OnFileSelected(FileSelectEventArgs<FileExplorerDirectoryContent> args)
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

    public void OnBeforeImageLoad(BeforeImageLoadEventArgs<FileExplorerDirectoryContent> args)
    {
        try
        {
            args.ImageUrl = args.FileDetails.TargetPath;
        }
        catch { }
    }

    public async Task OnBeforeDownload(BeforeDownloadEventArgs<FileExplorerDirectoryContent> args)
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

    private async Task ViewDeleted()
    {
        ShowDeleted = !ShowDeleted;
        SfFileManager.ShowHiddenItems = ShowDeleted;
        await Refresh();
    }

    //private async Task UploadDir()
    //{
    //    if (_FileUploader != null)
    //    {
    //        await _FileUploader.OpenInput(directoryUpload: true);
    //    }
    //}

    private async Task Refresh()
    {
        await JsRuntime.InvokeVoidAsync("CloseFileExplorerDialogs", FileManagerId);
        if (SfFileManager != null)
            await SfFileManager.RefreshFilesAsync();
    }

    private void OnItemsUploading(ItemsUploadEventArgs<FileExplorerDirectoryContent> args)
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

    private void OnSearching(SearchEventArgs<FileExplorerDirectoryContent> args)
    {
        args.Cancel = true;
    }

    private void OnRead(ReadEventArgs<FileExplorerDirectoryContent> args)
    {
        if (SfFileManager == null) return;
        SyncLocalStorage.RemoveItem(SfFileManager.ID);
    }

    private async Task OnCreated()
    {
        if (!string.IsNullOrWhiteSpace(CurrentPath) && SfFileManager != null)
        {
            var paths = CurrentPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var path in paths)
            {
                await SfFileManager.OpenFileAsync(path);
            }
        }
    }

    private void OnSuccess(SuccessEventArgs<FileExplorerDirectoryContent> args)
    {
        if (args.Result.Files?.Count > 0)
        {
            DeletedItemsCss = string.Join('\n', args.Result.Files
                .Where(x => x.IsDeleted)
                .Select(x => $".e-filemanager .e-list-parent [title='{x.Name}'] {{background-color: #ffc7c7;}}"));
        }

        this._FileUploader?.Items?.Clear();
    }

    public void OnSend(BeforeSendEventArgs args)
    {
        args.CustomData = [];

        if (!string.IsNullOrWhiteSpace(ContainerName))
        {
            args.CustomData.Add("ContainerName", ContainerName);
        }

        if (!string.IsNullOrWhiteSpace(AccountName))
        {
            args.CustomData.Add("AccountName", AccountName);
        }

        if (Root != null)
        {
            args.CustomData.Add("RootDir", Root);
        }
    }
}
