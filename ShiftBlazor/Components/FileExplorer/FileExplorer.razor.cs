using Microsoft.AspNetCore.Components;
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
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileExplorer : IShortcutComponent
{
    [Inject] ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] MessageService MessageService { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] IIdentityStore? TokenStore { get; set; }

    [CascadingParameter(Name = FormHelper.ParentReadOnlyName)]
    public bool? ParentReadOnly { get; set; }

    [CascadingParameter(Name = FormHelper.ParentDisabledName)]
    public bool? ParentDisabled { get; set; }

    [Parameter]
    public string? Root { get; set; }

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
    public bool DisableRecents { get; set; }

    [Parameter]
    public string? Height { get; set; }

    [Parameter]
    public bool ShowThumbnails { get; set; }

    [Parameter]
    public FileView? View { get; set; }

    [Parameter]
    public RenderFragment? MenuItemsTemplate { get; set; }

    [Parameter]
    public bool OpenDialogOnUpload { get; set; }

    [Parameter]
    public int MaxFileSizeInMegaBytes { get; set; } = 128;

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
    private List<string> PathParts = [];
    private FileExplorerDirectoryContent? LastSelectedFile { get; set; }
    private bool RenderQuickAccess => !DisableQuickAccess && Settings.QuickAccessItems.Count > 0;
    private string SortIcon => Settings.SortDescending ? Icons.Material.Filled.ArrowUpward : Icons.Material.Filled.ArrowDownward;
    private bool ShowDeletedFiles { get; set; }
    private bool DisplayDeleteButton { get; set; }
    private bool DisplayDownloadButton { get; set; }
    private bool DisplayQuickAccessButton { get; set; }
    private bool DisplayUploadButton { get; set; } = true;
    private bool DisplayNewFolderButton { get; set; } = true;
    private bool DisplayRestoreButton { get; set; }
    private bool DisplayContextMenu { get; set; }
    private double ContextLeft { get; set; }
    private double ContextTop { get; set; }
    private DotNetObjectReference<FileExplorer>? objRef;
    private readonly string URLPathKey = "path";
    private IEnumerable<string> ImageExtensions = new List<string>
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".png",
        ".webp",
    };

    private bool IsIconsView => Settings.View >= FileView.Small && Settings.View <= FileView.ExtraLarge;
    private string SettingKey => $"FileExplorer_{LoggedInUser?.ID}_{AccountName}_{ContainerName}_{Root}";
    public FileExplorerSettings Settings = DefaultAppSetting.FileExplorerSettings;
    private FileExplorerSettings DefaultSettings = DefaultAppSetting.FileExplorerSettings;
    TokenUserDataDTO? LoggedInUser;
    private Dictionary<string, string> Usernames = [];

    protected override void OnInitialized()
    {
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
        SetBreadcrumb();

        var userSettings = SettingManager.GetFileExplorerSetting(SettingKey);
        SetView(userSettings?.View ?? View ?? DefaultSettings.View);
        Settings = userSettings ?? DefaultSettings;
    }

    [JSInvokable]
    public async Task OnUrlChanged(string newUrl)
    {
        if (CWD == null) return;

        string urlPath = await JsRuntime.InvokeAsync<string>("getQueryParam", URLPathKey);
        string currentPath = CWD == null ? "" : (CWD.FilterPath == "" ? "" : CWD.FilterPath + CWD.Name);

        if (urlPath != currentPath)
        {
            await GoToPath(urlPath);
            StateHasChanged();
        }
    }

    public async Task UpdateUrlAsync()
    {
        if (CWD == null) return;

        string urlPath = await JsRuntime.InvokeAsync<string>("getQueryParam", URLPathKey);
        string currentPath = CWD == null ? "" : (CWD.FilterPath == "" ? "" : CWD.FilterPath + CWD.Name);

        if(currentPath != urlPath) 
            await JsRuntime.InvokeVoidAsync("updateQueryParams",
                new Dictionary<string, object>
                {
                    [URLPathKey] = currentPath
                }
            );
    }

    protected override async Task OnInitializedAsync()
    {
        objRef = DotNetObjectReference.Create(this);
        await JsRuntime.InvokeVoidAsync("addCustomUrlChangeListener", objRef, Id);

        if (TokenStore != null)
        {
            LoggedInUser = (await TokenStore.GetTokenAsync())?.UserData;
        }

        string urlPath = await JsRuntime.InvokeAsync<string>("getQueryParam", URLPathKey);

        if (String.IsNullOrWhiteSpace(urlPath)) await FetchData();
        else await GoToPath(urlPath);
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {
            case KeyboardKeys.KeyN:
                await CreateNewFolder();
                break;
            case KeyboardKeys.KeyU:
                await Upload();
                break;
            case KeyboardKeys.KeyS:
                Sort();
                break;
            case KeyboardKeys.KeyR:
                await Refresh(true);
                break;
            case KeyboardKeys.KeyD:
                await Delete();
                break;
            case KeyboardKeys.KeyT:
                await ViewDeletedFiles();
                break;
        }

        StateHasChanged();
    }

    public async Task FetchData(FileExplorerDirectoryContent? data = null)
    {
        IsLoading = true;

        LastSelectedFile = null;
        DeselectAllFiles();

        try
        {
            var obj = DefaultDirectoryContentObject();
            obj.Action = "read";
            obj.Path = GetPath(data);
            obj.Data = data == null ? [] : [data];
            obj.ShowHiddenItems = ShowDeletedFiles;

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

            if (content == null || content.CWD == null)
            {
                throw new Exception("Could not parse server data");
            }

            var files = content.Files?.ToList() ?? [];

            Files = files;
            CWD = content.CWD;
            var crumbPath = content.CWD.FilterPath == "" ? "" : content.CWD.FilterPath + content.CWD.Name;

            var userIds = files
                .Where(x => !string.IsNullOrWhiteSpace(x.CreatedBy))
                .Select(x => x.CreatedBy)
                .Distinct()
                .ToList();

            var users = await GetUsers(userIds);

            foreach (var file in files.Where(x => !string.IsNullOrWhiteSpace(x.CreatedBy)))
            {
                var user = users.FirstOrDefault(x => x.ID == file.CreatedBy);
                if (user?.ID != null)
                {
                    Usernames.TryAdd(user.ID, user.Name);
                }
            }

            SetBreadcrumb(crumbPath);
            SetSort();
            UpdateToolbarButtons();
            await UpdateUrlAsync();
        }
        catch (Exception e)
        {
            await DisplayError(e.Message);
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
            await Download(file);
        }
        else
        {
            await FetchData(file);
        }
    }

    private async Task OnFileClick(MouseEventArgs args, FileExplorerDirectoryContent file)
    {
        var isDoubleClick = args.Detail > 1;

        if (isDoubleClick)
        {
            await HandleOpen(file);
        }
        else if (args.ShiftKey)
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
            if (!SelectedFiles.Remove(file))
            {
                SelectedFiles.Add(file);
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
        DisplayNewFolderButton = CWD != null;
        DisplayUploadButton = CWD != null;

    }

    private async Task OnBreadCrumbClick(int index)
    {
        FileExplorerDirectoryContent? data = null;

        if (index > 0)
        {
            var path = string.Join("/", PathParts.GetRange(1, index));
            var filterPath = string.Join("/", PathParts.GetRange(1, index - 1));
            var name = PathParts[index];
            data = new FileExplorerDirectoryContent()
            {
                Path = path,
                Name = name,
                FilterPath = filterPath + "/",
                Type = "Directory",
            };

        }
        await FetchData(data);
    }

    private async Task CreateNewFolder()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
        };

        var result = await DialogService.Show<CreateFolderDialog>("", options).Result;

        try
        {
            if (result?.Data is string value)
            {
                DisplayContextMenu = false;
                var newFolderData = DefaultDirectoryContentObject();
                newFolderData.Action = "create";
                newFolderData.Path = GetPath(CWD);
                newFolderData.Name = value;
                newFolderData.Data = CWD == null ? [] : [CWD];

                var response = await HttpClient.PostAsJsonAsync(Url, newFolderData);

                var content = await response.Content.ReadFromJsonAsync<FileExplorerResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });

                if (content?.Error != null)
                {
                    throw new Exception(content.Error.Message);
                }

                await Refresh();
            }
        }
        catch (Exception e)
        {
            await DisplayError(e.Message);
        }
    }

    private async Task Upload()
    {
        if (_FileUploader != null)
        {
            await _FileUploader.OpenInput(directoryUpload: false);
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
            Loc["Delete File"],
            Loc["Are you sure you want to delete this file?"],
            yesText: Loc["Delete"], cancelText: Loc["Cancel"], options: options);
        
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

    private void AddToQuickAccess()
    {
        DisplayContextMenu = false;
        var file = SelectedFiles.LastOrDefault() ?? CWD;

        if (file == null || file.Path == null || file.IsFile) return;

        Settings.QuickAccessItems.Add(file.Path);

        SettingManager.SetFileExplorerSetting(SettingKey, Settings);
    }

    private void RemoveQuickAccessItem(string item)
    {
        if (Settings.QuickAccessItems.Remove(item))
        {
            SettingManager.SetFileExplorerSetting(SettingKey, Settings);
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
            var extension = Path.GetExtension(file.Name)?.ToLower();
            
            if (ImageExtensions.Contains(extension))
                return new(@Icons.Material.Filled.Image, "#dddddd");

            switch (extension)
            {
                case ".pdf":
                    return new(@Icons.Material.Filled.TextSnippet, "#de2429");
                case ".doc":
                case ".docx":
                    return new(@Icons.Material.Filled.TextSnippet, "#295294");
                case ".txt":
                    return new(@Icons.Material.Filled.TextSnippet, "#dddddd");
                case ".xls":
                case ".xlsx":
                    return new(@Icons.Material.Filled.ListAlt, "#3b885a");
                case ".csv":
                    return new(@Icons.Material.Filled.Archive, "#dddddd");
                case ".zip":
                case ".rar":
                case ".7z":
                    return new(@Icons.Material.Filled.Archive, "#f9ca40");
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

    private async Task RestoreFile()
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
            Loc["Restore File"],
            Loc["Are you sure you want to undelete this file?"],
            yesText: Loc["Restore"], cancelText: Loc["Cancel"], options: options);

        if (result == true)
        {
            DisplayContextMenu = false;
            var files = SelectedFiles.Where(x => x.IsDeleted).ToArray();
            var restoreData = DefaultDirectoryContentObject();
            restoreData.Action = "restore";
            restoreData.Path = SelectedFiles.First().FilterPath;
            restoreData.Data = files;

            var response = await HttpClient.PostAsJsonAsync(Url, restoreData);
            await Refresh();
        }
    }

    private string GetPath(FileExplorerDirectoryContent? data)
    {
        return data == null || string.IsNullOrWhiteSpace(data.FilterPath) ? "/" : data.FilterPath + data.Name;
    }

    private async Task<List<UserDetails>> GetUsers(List<string> userIds)
    {
        var filter = new ODataFilterGenerator()
            .Add(nameof(UserDetails.ID), ODataOperator.In, userIds)
            .ToString();
        var url = SettingManager.Configuration.UserListEndpoint + "?$filter=" + filter;

        var users = await HttpClient.GetFromJsonAsync<ODataDTO<UserDetails>>(url);
        return users?.Value ?? [];
    }

    private string GetViewClass(FileView? view = null)
    {
        return (view ?? Settings.View) switch
        {
            FileView.Small => "icons small",
            FileView.Medium => "icons medium",
            FileView.Large => "icons large",
            FileView.ExtraLarge => "icons extra-large",
            _ => ""
        };
    }

    private int GetImageSize(FileView? view = null)
    {
        return (view ?? Settings.View) switch
        {
            FileView.Small => 25,
            FileView.Medium => 70,
            FileView.Large => 100,
            FileView.ExtraLarge => 150,
            _ => 100
        };
    }

    public void SetView(FileView? view = null)
    {
        if (view == null)
        {
            // cycle through views enum
            var values = Enum.GetValues(typeof(FileView));
            var index = Array.IndexOf(values, Settings.View);
            Settings.View = (FileView)values.GetValue((index + 1) % values.Length)!;
        }
        else
        {
            Settings.View = view.Value;
        }

        SettingManager.SetFileExplorerSetting(SettingKey, Settings);
    }

    private void HandleUploading(UploadEventArgs args)
    {
        UploadingFiles = args;
    }

    private void Sort()
    {
        SortBy(Settings.Sort);
        StateHasChanged();
    }

    public void SortBy(FileSort sort, bool? isDescending = null)
    {
        Settings.SortDescending = isDescending != null
            ? isDescending.Value
            : Settings.Sort == sort && !Settings.SortDescending;

        Settings.Sort = sort;
        SettingManager.SetFileExplorerSetting(SettingKey, Settings);
        SetSort();
    }

    private void SetSort()
    {
        var direction = Settings.SortDescending ? SortDirection.Descending : SortDirection.Ascending;

        switch (Settings.Sort)
        {
            case FileSort.Name:
                Files = Files.OrderByDirection(direction, x => x.Name).ToList();
                break;
            case FileSort.Date:
                Files = Files.OrderByDirection(direction, x => x.DateModified).ToList();
                break;
            case FileSort.Size:
                Files = Files.OrderByDirection(direction, x => x.Size).ToList();
                break;
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

    private string SpecialItemClasses(FileExplorerDirectoryContent file)
    {
        var classes = new List<string>();
        if (SelectedFiles.Any(x => x.Path == file.Path))
        {
            classes.Add("selected");
        }

        if (file.IsDeleted)
        {
            classes.Add("deleted");
        }
        return string.Join(" ", classes);
    }

    private async Task DisplayError(string message)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = true,
            CloseOnEscapeKey = true,
        };

        await DialogService.ShowMessageBox("Error", message ?? "Could not parse server data", yesText: "Ok", options: options);
    }

    public void Dispose()
    {
        IShortcutComponent.Remove(Id);
        JsRuntime.InvokeVoidAsync("removeCustomUrlChangeListener", Id);
        objRef?.Dispose();   
    }
}
