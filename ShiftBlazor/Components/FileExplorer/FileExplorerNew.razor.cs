﻿using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileExplorerNew : IShortcutComponent
{
    [Inject] ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] MessageService MessageService { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] ISyncLocalStorageService SyncLocalStorage { get;set; } = default!;
    [Inject] NavigationManager NavManager { get; set; } = default!;

    [CascadingParameter(Name = FormHelper.ParentReadOnlyName)]
    public bool? ParentReadOnly { get; set; }

    [CascadingParameter(Name = FormHelper.ParentDisabledName)]
    public bool? ParentDisabled { get; set; }

    [Parameter]
    public string? Root {  get; set; }

    [Parameter]
    public string? CurrentPath { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? AccountName { get; set; }

    [Parameter]
    public string? ContainerName { get; set; }

    [Parameter]
    public string? NavColor { get; set; }

    [Parameter]
    public bool NavIconFlatColor { get; set; }

    [Parameter]
    public bool Outlined { get; set; }

    [Parameter]
    public bool Dense { get; set; }

    [Parameter]
    public string IconSvg { get; set; } = @Icons.Material.Filled.List;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? RootAliasName { get; set; }

    [Parameter]
    public bool DisableQuickAccess { get; set; }

    [Parameter]
    public bool DisableRecents{ get; set; }

    [Parameter]
    public string? Height { get; set; }

    [Parameter]
    public bool ShowThumbnails { get; set; }

    [Parameter]
    public FileExplorerView? View { get;set; }

    [Parameter]
    public RenderFragment? MenuItemsTemplate { get; set; }
    
    [Parameter]
    public bool OpenDialogOnUpload { get; set; }

    [Parameter]
    public bool DisableUrlSync { get; set; }
    [Parameter]
    public bool DisableSortUrlSync { get; set; }
    [Parameter]
    public bool DisablePathUrlSync { get; set; }

    public bool IsEmbed { get; private set; } = false;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();
    public List<FileExplorerDirectoryContent> SelectedFiles { get; set; } = [];

    private string FileExplorerId { get; set; }
    private string ToolbarStyle = string.Empty;
    private Size IconSize = Size.Medium;
    private bool DisableSidebar => DisableQuickAccess && DisableRecents;
    private FileExplorerDirectoryContent? CWD { get; set; } = null;
    private List<FileExplorerDirectoryContent> Files { get; set; } = new();
    private UploadEventArgs? UploadingFiles { get; set; }
    private bool IsLoading { get; set; } = true;
    private string Url = "";
    private FileUploader? _FileUploader { get; set; }
    private List<string> QuickAccessFiles { get; set; } = [];
    private List<string> PathParts = [];
    private FileExplorerDirectoryContent? LastSelectedFile { get; set; }
    private bool RenderQuickAccess => !DisableQuickAccess && QuickAccessFiles.Count > 0;
    private bool ShowDeletedFiles { get; set; }
    private ShiftSortDirection SortType;
    private bool DisplayDeleteButton { get; set; }
    private bool DisplayDownloadButton { get; set; }
    private bool DisplayQuickAccessButton { get; set; }
    private bool DisplayUploadButton { get; set; } = true;
    private bool DisplayNewFolderButton { get; set; } = true;
    private bool DisplayRestoreButton { get; set; }
    private FileExplorerView CurrentView { get; set; } = FileExplorerView.LargeIcons;
    private bool DisplayContextMenu { get; set; }
    private double ContextLeft { get; set; }
    private double ContextTop { get; set; }
    private bool SuppressLocationChange = false;

    private IEnumerable<string> ImageExtensions = new List<string>
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".png",
        ".webp",
    };

    public void Dispose()
    {
        NavManager.LocationChanged -= OnLocationChanged;
    }
    protected override void OnInitialized()
    {
        if (!DisableUrlSync)
        {
            NavManager.LocationChanged += OnLocationChanged;
            SyncFromUrl(null, null);
        }

        IsEmbed = ParentDisabled != null || ParentReadOnly != null;

        if (!IsEmbed)
        {
            IShortcutComponent.Register(this);
        }
        var apiUrl = BaseUrl
            ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "")
            ?? SettingManager.Configuration.ApiPath;

        Url = apiUrl.AddUrlPath("FileExplorer", "FileOperations");

        FileExplorerId = "FileExplorer" + Id.ToString().Replace("-", string.Empty);
        ToolbarStyle = $"{ColorHelperClass.GetToolbarStyles(NavColor, NavIconFlatColor)}border: 0;";
        IconSize = Dense ? Size.Medium : Size.Large;
        SetView(View ?? CurrentView);
        SetBreadcrumb();
        GetQuickAccessItems();
    }
    private async void OnLocationChanged(object? sender, LocationChangedEventArgs? args)
    {
        if (SuppressLocationChange)
        {
            SuppressLocationChange = false;
            return;
        }

        SyncFromUrl(sender, args); 
        await Refresh();    
        StateHasChanged();    
    }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(CurrentPath)) await GoToPath(CurrentPath);
        else await FetchData();
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {

        }
    }

    private FileExplorerDirectoryContent DefaultDirectoryContentObject()
    {
        var CustomData = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(ContainerName))
        {
            CustomData.Add("ContainerName", ContainerName);
        }

        if (!string.IsNullOrWhiteSpace(AccountName))
        {
            CustomData.Add("AccountName", AccountName);
        }

        if (Root != null)
        {
            CustomData.Add("RootDir", Root);
        }

        return new FileExplorerDirectoryContent
        {
            CustomData = CustomData,
        };
    }

    public async Task FetchData(FileExplorerDirectoryContent? data = null)
    {
        IsLoading = true;
        LastSelectedFile = null;
        DeselectAllFiles();

        try
        {
            var obj = DefaultDirectoryContentObject();
            if (data != null) data.SortType = SortType.ToString().ToLower();
            obj.Action = "read";
            obj.Path = GetPath(data);
            obj.Data = data == null ? [] : [data];
            obj.ShowHiddenItems = ShowDeletedFiles;
            obj.SortType = SortType.ToString().ToLower();

            var response = await HttpClient.PostAsJsonAsync(Url, obj);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception();
            }

            var content = await response.Content.ReadFromJsonAsync<FileExplorerResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            if (content?.Error != null)
            {
                throw new Exception(content.Error.Message);
            }

            var files = content?.Files?.ToList() ?? new List<FileExplorerDirectoryContent>();

            foreach (var file in files)
            {
                file.UploadProgress = 1;
            }

            Files = files;
            CWD = content?.CWD;
            var crumbPath = content.CWD.FilterPath == "" ? "" : content.CWD.FilterPath + content.CWD.Name;
            SetBreadcrumb(crumbPath);
            SuppressLocationChange = true;
            updateURL();
        }
        catch (Exception e)
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                BackdropClick = true,
                CloseOnEscapeKey = true,
            };

            await DialogService.ShowMessageBox("Error", e.Message ?? "Could not parse server data", yesText: "Ok", options: options);
        }

        IsLoading = false;
    }

    private void SetBreadcrumb(string path = "")
    {
        var breadcrumb = new List<string>
        {
            RootAliasName ?? "Root",
        };

        breadcrumb.AddRange(path.Split('/', StringSplitOptions.RemoveEmptyEntries));

        PathParts = breadcrumb;
    }
    
    private async Task HandleOpen(FileExplorerDirectoryContent file)
    {
        if (file.IsFile)
        {
            Download(file);
        }
        else
        {
            await FetchData(file);
        }
    }

    private void OnFileClick(MouseEventArgs args, FileExplorerDirectoryContent file)
    {
        if (args.ShiftKey)
        {
            var lastFileIndex = Files.IndexOf(LastSelectedFile ?? Files.First());
            var clickedIndex = Files.IndexOf(file);
            var i = lastFileIndex < clickedIndex ? lastFileIndex : clickedIndex;
            var count = Math.Abs(lastFileIndex - clickedIndex) + 1;

            var filesToSelect = Files.GetRange(i, count);
            if (!args.CtrlKey)
            {
                SelectedFiles.Clear();
            }
            else
            {
                filesToSelect = filesToSelect.Where(f => !SelectedFiles.Contains(f)).ToList();
            }

            SelectedFiles.AddRange(filesToSelect);
        }
        else if (args.CtrlKey)
        {
            if (!SelectedFiles.Contains(file))
            {
                SelectedFiles.Add(file);
            }
            else
            {
                SelectedFiles.Remove(file);
            }

            LastSelectedFile = file;
        }
        else
        {
            LastSelectedFile = file;
            SelectedFiles = [file];
        }

        UpdateToolbarButtons();
    }

    public void DeselectAllFiles()
    {
        SelectedFiles.Clear();
        UpdateToolbarButtons();
    }

    private void UpdateToolbarButtons()
    {
        DisplayDeleteButton = SelectedFiles.Count > 0 && !SelectedFiles.Any(x => x.IsDeleted);
        DisplayDownloadButton = SelectedFiles.Count > 0 && SelectedFiles.Any(x => x.IsFile);
        DisplayQuickAccessButton = !DisableQuickAccess && SelectedFiles.Count > 0 && SelectedFiles.All(x => !x.IsFile);
        DisplayRestoreButton = SelectedFiles.Count > 0 && SelectedFiles.Any(x => x.IsDeleted);
        //DisplayNewFolderButton = SelectedFiles.Count == 0;
        //DisplayUploadButton = SelectedFiles.Count == 0;

    }

    private async Task OnBreadCrumbClick(int index)
    {
        var path = string.Join("/", PathParts.GetRange(1, index));

        await GoToPath("/" + path);
    }

    private async Task CreateNewFolder()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
        };

        var result = await DialogService.Show<CreateFolderDialog>("", options).Result;
        
        if (result?.Data is string value)
        {
            DisplayContextMenu = false;
            var newFolderData = DefaultDirectoryContentObject();
            newFolderData.Action = "create";
            newFolderData.Path = GetPath(CWD);
            newFolderData.Name = value;
            newFolderData.Data = CWD == null ? [] : [CWD];

            var response = await HttpClient.PostAsJsonAsync(Url, newFolderData);
            await Refresh();
        }
    }

    private async Task Upload()
    {
        if (_FileUploader != null)
        {
            await _FileUploader.OpenInput(directoryUpload: false);
        }
    }

    private void Sort()
    {
        SortType = SortType switch
        {
            ShiftSortDirection.None => ShiftSortDirection.Asc,
            ShiftSortDirection.Asc => ShiftSortDirection.Desc,
            ShiftSortDirection.Desc => ShiftSortDirection.None,
            _ => ShiftSortDirection.None
        };

        updateURL();
    }

    private void updateURL()
    {
        if (DisableUrlSync) return;
             
        var uri = new Uri(NavManager.Uri);
        var baseUri = uri.GetLeftPart(UriPartial.Path);
        var query = HttpUtility.ParseQueryString(uri.Query);

        if(!DisableSortUrlSync) query.Set("sort", SortType.ToString().ToLower());

        if (!DisablePathUrlSync)  query.Set("path", CWD != null && CWD.FilterPath == "" ? "" : CWD.FilterPath + CWD.Name);

        var updatedUri = $"{baseUri}?{query}";
        NavManager.NavigateTo(updatedUri, forceLoad: false);
    }

    private string GetSortIcon()
    {
        return SortType switch
        {
            ShiftSortDirection.None => Icons.Material.Filled.UnfoldMore,
            ShiftSortDirection.Asc => Icons.Material.Filled.ArrowUpward,
            ShiftSortDirection.Desc => Icons.Material.Filled.ArrowDownward,
            _ => Icons.Material.Filled.UnfoldMore
        };
    }

    private void SyncFromUrl(object? sender, LocationChangedEventArgs? args)
    {
        var uri = new Uri(NavManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);


        if (!DisableSortUrlSync)
        {
            var sortParam = query.Get("sort");
            if (Enum.TryParse<ShiftSortDirection>(sortParam, true, out var parsedSort))  SortType = parsedSort;
        }

        if (!DisablePathUrlSync)
        {
            var pathParam = query.Get("path");
            if(!string.IsNullOrWhiteSpace(pathParam))  CurrentPath = pathParam.Trim();
        }

    }

    public async Task Refresh(bool force = false)
    {
        if (force)
        {
            Files = [];
        }
        DisplayContextMenu = false;
        //await _FileUploader.ClearAll();
        await FetchData(CWD);
    }

    private async Task Delete()
    {
        if (SelectedFiles.Count == 0)
        {
            return;
        }

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
        };

        bool? result = await DialogService.ShowMessageBox(
            "Delete File",
            "Are you sure you want to delete this file?",
            yesText: "Delete", cancelText: "Cancel", options: options);
        
        if (result == true)
        {
            DisplayContextMenu = false;
            var files = SelectedFiles.Where(x => !x.IsDeleted).ToArray();
            var deleteData = DefaultDirectoryContentObject();
            deleteData.Action = "delete";
            deleteData.Path = SelectedFiles.First().FilterPath;
            deleteData.Data = files;

            var response = await HttpClient.PostAsJsonAsync(Url, deleteData);
            await Refresh();
        }
    }

    private async Task Download(FileExplorerDirectoryContent? file = null)
    {
        IEnumerable<FileExplorerDirectoryContent> files = [];
        DisplayContextMenu = false;

        if (file != null)
        {
            files = [file];
        }
        else if (SelectedFiles.Count > 0)
        {
            files = SelectedFiles.Where(x => x.IsFile);
        }

        foreach (var f in files)
        {
            await JsRuntime.InvokeVoidAsync("downloadFileFromUrl", f.Name, f.TargetPath);
        }
    }

    private void ContextMenu(MouseEventArgs args)
    {
        DisplayContextMenu = true;
        ContextLeft = args.ClientX;
        ContextTop = args.ClientY;
    }

    public void CloseContextMenu()
    {
        DisplayContextMenu = false;
    }

    public void GetQuickAccessItems()
    {
        var items = SyncLocalStorage.GetItem<List<string>>("QuickAccess");

        QuickAccessFiles = items ?? [];
    }

    private void AddToQuickAccess()
    {
        DisplayContextMenu = false;
        var file = SelectedFiles.LastOrDefault() ?? CWD;

        if (file == null || file.Path == null || file.IsFile) return;

        var items = SyncLocalStorage.GetItem<List<string>>("QuickAccess");

        items ??= [];

        items.Add(file.Path);

        SyncLocalStorage.SetItem("QuickAccess", items);

        GetQuickAccessItems();
    }

    private void RemoveQuickAccessItem(string item)
    {
        var items = SyncLocalStorage.GetItem<List<string>>("QuickAccess");

        if (items?.Contains(item) == true)
        {
            items.Remove(item);
            SyncLocalStorage.SetItem("QuickAccess", items);
            QuickAccessFiles = items;
        }
    }

    private async Task GoToPath(string path)
    {
        var i = path.LastIndexOf('/');
        var filterPath = path.Substring(0, i + 1);
        var name = path.Substring(i + 1);

        var data = new FileExplorerDirectoryContent
        {
            FilterPath = filterPath,
            Name = name,
            Path = path,
        };

        await FetchData(data);
    }

    private KeyValuePair<string, string> GetFileIcon(FileExplorerDirectoryContent file)
    {
        if (file.IsFile)
        {
            switch(Path.GetExtension(file.Name))
            {
                case ".pdf":
                case ".doc":
                case ".docx":
                case ".txt":
                    return new(@Icons.Material.Filled.TextSnippet, "#ff0000");

                default:
                    return new(Icons.Material.Filled.InsertDriveFile, "#dddddd");
            }
        }
        else
        {
            return new (Icons.Material.Filled.Folder, "#f1ce69");
        }

    }

    private async Task ViewDeletedFiles()
    {
        ShowDeletedFiles = !ShowDeletedFiles;
        await Refresh();
    }

    private void RestoreFile()
    {
        // restore file
    }

    private string GetPath(FileExplorerDirectoryContent? data)
    {
        return data == null || string.IsNullOrWhiteSpace(data.FilterPath) ? "/" : data.FilterPath + data.Name;
    }

    private string GetViewClass(FileExplorerView? view = null)
    {
        switch (view ?? CurrentView)
        {
            case FileExplorerView.LargeIcons:
                return "large-icons";
            case FileExplorerView.Information:
                return "information";
            default:
                return "large-icons";
        }
    }

    public void SetView(FileExplorerView? view = null)
    {
        if (view == null)
        {
            // cycle through views enum
            var values = Enum.GetValues(typeof(FileExplorerView));
            var index = Array.IndexOf(values, CurrentView);
            CurrentView = (FileExplorerView)values.GetValue((index + 1) % values.Length)!;
        }
        else
        {
            CurrentView = view.Value;
        }
    }

    private void HandleUploading(UploadEventArgs args)
    {
        UploadingFiles = args;
    }

    public enum FileExplorerView
    {
        LargeIcons,
        Information,
    }

    void IDisposable.Dispose()
    {
        IShortcutComponent.Remove(Id);
    }
}
