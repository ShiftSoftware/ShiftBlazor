using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.FileExplorer.Dtos;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.TypeAuth.Core;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileExplorer : IShortcutComponent, IRequestComponent
{
    [Inject] public ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] public SettingManager SettingManager { get; set; } = default!;
    [Inject] public HttpClient HttpClient { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] ISnackbar Snackbar { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;


    [CascadingParameter(Name = FormHelper.ParentReadOnlyName)]
    public bool? ParentReadOnly { get; set; }

    [CascadingParameter(Name = FormHelper.ParentDisabledName)]
    public bool? ParentDisabled { get; set; }

    [Parameter]
    public string? Root { get; set; }

    public string? Endpoint { get; }

    [Parameter]
    public string? CurrentPath { get; set; }

    [Parameter]
    public string URLPathKey { get; set; } = "path";

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
    public RenderFragment? ToolbarTemplate { get; set; }

    [Parameter]
    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }
    [Parameter]
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }
    [Parameter]
    public Func<Exception, ValueTask<bool>>? OnError { get; set; }

    [Parameter]
    public bool OpenDialogOnUpload { get; set; }

    [Parameter]
    public int MaxFileSizeInMegaBytes { get; set; } = 128;

    [Parameter]
    [Obsolete("This parameter is not used anymore.", false)]
    public int MaxUploadFileCount { get; set; } = 16;

    public bool IsEmbed { get; private set; } = false;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = [];
    public List<FileExplorerItemDTO> SelectedFiles { get; set; } = [];

    private string FileExplorerId => "FileExplorer" + Id.ToString().Replace("-", string.Empty);
    private string ToolbarStyle = string.Empty;
    private Size IconSize = Size.Medium;
    private bool DisableSidebar => DisableQuickAccess && DisableRecents;
    private FileExplorerResponseDTO? CWD { get; set; }
    private List<FileExplorerItemDTO> Files { get; set; } = [];
    private List<FileExplorerItemDTO>? FilteredFiles { get; set; }
    private List<FileExplorerItemDTO> DisplayedFiles => FilteredFiles ?? Files;
    private UploadEventArgs? UploadingFiles { get; set; }
    private bool IsLoading { get; set; } = true;
    private string Url = "";
    private FileUploader? _FileUploader { get; set; }
    private List<string> PathParts = [];
    private FileExplorerItemDTO? LastSelectedFile { get; set; }
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
    private bool DisplayDeleteToggle { get; set; }
    private bool DisplayDetailsButton { get; set; }
    private bool IsContextMenuEmpty { get; set; }

    private double ContextLeft { get; set; }
    private double ContextTop { get; set; }

    private bool IsIconsView => Settings.View >= FileView.Small && Settings.View <= FileView.ExtraLarge;
    private string SettingKey => $"FileExplorer_{LoggedInUser?.ID}_{AccountName}_{ContainerName}_{Root}";
    public FileExplorerSettings Settings = DefaultAppSetting.FileExplorerSettings;
    private FileExplorerSettings DefaultSettings = DefaultAppSetting.FileExplorerSettings;
    TokenUserDataDTO? LoggedInUser;
    private Dictionary<string, string> Usernames = [];
    private ITypeAuthService? TypeAuthService;
    private string SearchQuery { get; set; } = string.Empty;
    internal static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
    };
    internal static readonly Dictionary<string, (string icon, string color)> FileIcons =
    new[]
    {
        (Extensions: new[] { ".pdf" }, Value: ("picture_as_pdf", "#de2429")),
        (Extensions: new[] { ".doc", ".docx" }, Value: ("docs", "#295294")),
        (Extensions: new[] { ".txt", ".rtf" }, Value: ("description", "#777777")),
        (Extensions: new[] { ".xls", ".xlsx" }, Value: ("table", "#3b885a")),
        (Extensions: new[] { ".ppt", ".pptx" }, Value: ("wallpaper_slideshow", "#d14b4b")),
        (Extensions: new[] { ".csv" }, Value: ("csv", "#3b885a")),
        (Extensions: new[] { ".zip", ".rar", ".7z" }, Value: ("archive", "#f9ca40")),
        (Extensions: new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" }, Value: ("audio_file", "#6dabfb")),
        (Extensions: new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg" }, Value: ("video_file", "#d15eff")),
        (Extensions: new[] { ".html", ".htm", ".css", ".js", ".php", ".ts", ".py", ".java", ".c", ".cpp", ".cs" }, Value: ("code", "#001234")),
        (Extensions: new[] { ".json", ".xml" }, Value: ("data_array", "#777777")),
        (Extensions: new[] { ".exe", ".dll", ".msi" }, Value: ("terminal", "#001234")),
        (Extensions: new[] { ".sh", ".bat", ".cmd", ".ps1" }, Value: ("terminal", "#001234")),
        (Extensions: new[] { ".apk", ".apks", ".aab", ".xapk", ".apkm", ".akp" }, Value: ("apk_document", "#3DDC84")),
        (Extensions: [..ImageExtensions], Value: ("image", "#d14b4b")),
        (Extensions: new[] { "" }, Value: ("draft", "#777777")),
        (Extensions: new[] { "folder" }, Value: ("folder", "#f1ce69")),
        (Extensions: new[] { "files" }, Value: ("stacks", "#F9F9F9")),

    }
    .SelectMany(group => group.Extensions.Select(ext => (ext, group.Value)))
    .ToDictionary(x => x.ext, x => x.Value);

    // the list needs to be sorted alphabetically for the Google Fonts icons to work properly
    private static readonly string GoogleFontsIconNames = string.Join(",", FileIcons.Values.Select(static x => x.icon).Distinct().OrderBy(x => x));

    protected override void OnInitialized()
    {
        IsEmbed = ParentDisabled != null || ParentReadOnly != null;

        if (!IsEmbed)
        {
            IShortcutComponent.Register(this);
        }

        string? url = BaseUrl;
        var config = SettingManager.Configuration;

        if (url is null && BaseUrlKey is not null)
            url = config.ExternalAddresses.TryGet(BaseUrlKey);

        url ??= config.BaseAddress;
        Url = url.AddUrlPath("FileExplorer");

        ToolbarStyle = $"{ColorHelperClass.GetToolbarStyles(NavColor, NavIconFlatColor)}border: 0;";
        IconSize = Dense ? Size.Medium : Size.Large;

        TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();
        DisplayDeleteToggle = TypeAuthService?.CanAccess(AzureStorageActionTree.ViewDeletedFiles) != false;

        NavigationManager.LocationChanged += LocationChanged;
    }


    private void LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        if (CWD?.Path == null) return;

        var uri = new Uri(e.Location);
        var urlPath = HttpUtility.ParseQueryString(uri.Query).Get(URLPathKey) ?? string.Empty;

        if (urlPath != GetFriendlyPath(CWD.Path))
        {
            GoToPath(GetRoot() + urlPath).ContinueWith(x => StateHasChanged());
        }
    }

    public async Task UpdateUrlAsync()
    {
        if (CWD?.Path == null) return;

        string urlPath = await JsRuntime.InvokeAsync<string>("getQueryParam", URLPathKey);
        var dirPath = GetFriendlyPath(CWD.Path);
        var updatePath = (string.IsNullOrWhiteSpace(dirPath) && !string.IsNullOrWhiteSpace(urlPath))
            || (!string.IsNullOrWhiteSpace(dirPath) && dirPath != urlPath);

        if (updatePath)
            await JsRuntime.InvokeVoidAsync("updateQueryParams",
                new Dictionary<string, object>
                {
                    [URLPathKey] = dirPath
                }
            );
    }

    protected override async Task OnInitializedAsync()
    {
        var tokenStore = ServiceProvider.GetService<IIdentityStore>();

        if (tokenStore != null)
        {
            LoggedInUser = (await tokenStore.GetTokenAsync())?.UserData;
        }

        var userSettings = SettingManager.GetFileExplorerSetting(SettingKey);
        Settings = userSettings ?? DefaultSettings;
        SetView(userSettings?.View ?? View ?? DefaultSettings.View, false);

        var urlPath = await JsRuntime.InvokeAsync<string?>("getQueryParam", URLPathKey);

        if (!string.IsNullOrWhiteSpace(urlPath))
            await GoToPath(GetRoot() + urlPath);
        else if (!string.IsNullOrWhiteSpace(CurrentPath))
            await GoToPath(GetRoot() + CurrentPath);
        else
            await FetchData();
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {
            case KeyboardKeys.KeyN:
                if (DisplayNewFolderButton)
                await CreateNewFolder();
                break;
            case KeyboardKeys.KeyU:
                if (DisplayUploadButton)
                await Upload();
                break;
            case KeyboardKeys.KeyS:
                Sort();
                break;
            case KeyboardKeys.KeyR:
                await Refresh(true);
                break;
            case KeyboardKeys.KeyD:
                if (DisplayDeleteButton)
                    await Delete();
                break;
            case KeyboardKeys.KeyT:
                await ViewDeletedFiles();
                break;
        }

        StateHasChanged();
    }

    //private string PreparePath(string? path = null)
    //{
    //    if (string.IsNullOrWhiteSpace(Root))
    //        return path ?? "";

    //    return Root.Trim('/') + "/" + path;
    //}

    //private string StrippedPath(string path)
    //{
    //    if (string.IsNullOrWhiteSpace(Root))
    //        return path;
    //    if (path.StartsWith(Root))
    //        return path[Root.Length..].TrimStart('/');
    //    return path;
    //}

    private string GetRoot()
    {
        if (string.IsNullOrWhiteSpace(Root))
            return "";
        if (!Root.EndsWith('/'))
            Root += "/";
        return Root.TrimStart('/');
    }

    private string GetFriendlyPath(string path)
    {
        var root = GetRoot();
        if (!string.IsNullOrWhiteSpace(root))
            return path.Replace(root, "");
        return path;
    }

    public async Task FetchData(string? path = null)
    {
        await FetchData(new FileExplorerReadDTO { Path = path });
    }

    private string CreateQuery(FileExplorerRequestDTOBase data)
    {
        data.AccountName ??= AccountName;
        data.ContainerName ??= ContainerName;

        var props = data.GetType().GetProperties();
        var queries = props.Select(prop => prop.Name + "=" + prop.GetValue(data));
        return string.Join("&", queries);
    }

    public async Task FetchData(FileExplorerReadDTO? data)
    {
        IsLoading = true;
        LastSelectedFile = null;
        DeselectAllFiles();
        try
        {
            data ??= new();
            data.Path ??= GetRoot();
            data.IncludeDeleted = ShowDeletedFiles;
            var query = CreateQuery(data);
            var url = new Uri($"{Url.AddUrlPath("list")}?{query}");

            using var requestMessage = HttpClient.CreateRequestMessage(HttpMethod.Get, url);

            if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                return;

            using var response = await HttpClient.SendAsync(requestMessage);

            if (OnResponse != null && !(await OnResponse.Invoke(response)))
                return;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch data");
            }

            var content = await response.Content.ReadFromJsonAsync<FileExplorerResponseDTO>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            if (content == null || content.Path == null)
            {
                throw new Exception("Could not parse server data");
            }


            if (content.Message != null)
            {
                throw new Exception(content.Message.Title);
            }

            var files = content.Items?.ToList() ?? [];

            // if we are fetching additional items (Load More button)
            // then add the new items to the current list instead of replacing
            if (string.IsNullOrWhiteSpace(data.ContinuationToken))
            {
                Files = files;
            }
            else
            {
                Files.AddRange(files);
            }

            CWD = content;

            var userIds = files
                .Where(x => !string.IsNullOrWhiteSpace(x.CreatedBy))
                .Select(x => x.CreatedBy!)
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
            
            SetBreadcrumb(CWD.Path);
            SetSort();
            UpdateToolbarButtons();
            await UpdateUrlAsync();
        }
        catch (Exception e)
        {
            if (OnError != null && !(await OnError.Invoke(e)))
                return;
            DisplayError(e.Message);
        }

        IsLoading = false;
    }

    private void SetBreadcrumb(string path = "")
    {
        var breadcrumb = new List<string>
        {
            RootAliasName ?? "Root",
        };

        breadcrumb.AddRange(GetFriendlyPath(path).Split('/', StringSplitOptions.RemoveEmptyEntries));
        PathParts = breadcrumb;
    }
    
    private async Task HandleOpen(FileExplorerItemDTO file)
    {
        if (file.IsFile)
        {
            await Download(file);
        }
        else
        {
            ClearSearch();
            await FetchData(file.Path);
        }
    }

    private async Task OnFileClick(MouseEventArgs args, FileExplorerItemDTO file)
    {
        var isDoubleClick = args.Detail > 1;
        var isRightClick = args.Button == 2;

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
            if (!isRightClick || !SelectedFiles.Contains(file))
            {
                SelectedFiles = [file];
            }
            LastSelectedFile = file;
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
        DisplayDetailsButton = SelectedFiles.Count > 0;
        DisplayNewFolderButton = CWD?.Path != null;
        DisplayUploadButton = CWD?.Path != null;

        IsContextMenuEmpty = !(DisplayDeleteButton
                            || DisplayDownloadButton
                            || DisplayQuickAccessButton
                            || DisplayRestoreButton
                            || DisplayDetailsButton);
    }

    private async Task OnBreadCrumbClick(int index)
    {
        string? path = null;

        if (index > 0)
        {
            path = GetRoot() + string.Join("/", PathParts.GetRange(1, index)) + "/";
        }
        await FetchData(path);
    }

    private async Task CreateNewFolder()
    {
        DisplayContextMenu = false;
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
        };

        var dialogRef = await DialogService.ShowAsync<CreateFolderDialog>("", options);
        var result = await dialogRef.Result;

        try
        {
            if (result?.Data is string value)
            {
                var newFolderData = new FileExplorerCreateDTO
                {
                    Path = (CWD?.Path ?? "").AddUrlPath(value) + "/",
                    AccountName = AccountName,
                    ContainerName = ContainerName,
                };

                var url = Url.AddUrlPath("create");

                using var requestMessage = HttpClient.CreatePostRequest(newFolderData, url);

                if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                    return;

                using var response = await HttpClient.SendAsync(requestMessage);

                if (OnResponse != null && !(await OnResponse.Invoke(response)))
                    return;

                var content = await response.Content.ReadFromJsonAsync<FileExplorerResponseDTO>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });

                if (content?.Message != null)
                {
                    throw new Exception(content.Message.Title);
                }

                await Refresh();
            }
        }
        catch (Exception e)
        {
            if (OnError != null && !(await OnError.Invoke(e)))
                return;
            DisplayError(e.Message);
        }
    }

    private async Task Upload()
    {
        if (_FileUploader != null)
        {
            this._FileUploader.Items.Clear();
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
        await FetchData(CWD?.Path);
    }

    private async Task Delete()
    {
        DisplayContextMenu = false;

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
            yesText: Loc["Delete"], cancelText: Loc["CancelChanges"], options: options);
        
        if (result == true)
        {

            try
            {
                var deleteData = new FileExplorerDeleteDTO
                {
                    Paths = SelectedFiles.Select(x => x.Path!).ToArray(),
                    AccountName = AccountName,
                    ContainerName = ContainerName,
                };
                var url = Url.AddUrlPath("delete");

                using var requestMessage = HttpClient.CreatePostRequest(deleteData, url);

                if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                    return;

                using var response = await HttpClient.SendAsync(requestMessage);

                if (OnResponse != null && !(await OnResponse.Invoke(response)))
                    return;

                await Refresh();
            }
            catch (Exception e) 
            {
                if (OnError != null && !(await OnError.Invoke(e)))
                    return;
                DisplayError(e.Message);
            }
        }
    }

    private async Task Download(FileExplorerItemDTO? file = null)
    {
        IEnumerable<FileExplorerItemDTO> files = [];
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
            await JsRuntime.InvokeVoidAsync("downloadFileFromUrl", f.Name, f.Url);
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
        var file = SelectedFiles.LastOrDefault();

        if (file?.IsFile == true)
            return;

        var path = file?.Path ?? CWD?.Path;

        if (path == null)
            return;

        Settings.QuickAccessItems.Add(path);

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
        await FetchData(path);
    }

    internal static (string icon, string color) GetFileIcon(FileExplorerItemDTO file)
    {
        if (file.IsFile)
        {
            var extension = Path.GetExtension(file.Name)?.ToLower() ?? string.Empty;
            
            if (FileIcons.TryGetValue(extension, out var value))
            {
                return value;
            }

            return FileIcons[string.Empty];
        }
        else
        {
            return FileIcons["folder"];
        }
    }

    private async Task ViewDeletedFiles()
    {
        ShowDeletedFiles = !ShowDeletedFiles;
        await Refresh(!ShowDeletedFiles);
    }

    private async Task RestoreFile()
    {
        DisplayContextMenu = false;
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
            yesText: Loc["Restore"], cancelText: Loc["CancelChanges"], options: options);

        if (result == true)
        {
            try
            {
                var restoreData = new FileExplorerRestoreDTO
                {
                    Paths = SelectedFiles.Select(x => x.Path!).ToArray(),
                    AccountName = AccountName,
                    ContainerName = ContainerName,
                };
                var url = Url.AddUrlPath("restore");

                using var requestMessage = HttpClient.CreatePostRequest(restoreData, url);

                if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                    return;

                using var response = await HttpClient.SendAsync(requestMessage);

                if (OnResponse != null && !(await OnResponse.Invoke(response)))
                    return;

                await Refresh();
            }
            catch (Exception e)
            {
                if (OnError != null && !(await OnError.Invoke(e)))
                    return;
                DisplayError(e.Message);
            }
        }
    }

    private async Task GetDetails()
    {
        DisplayContextMenu = false;
        var path = SelectedFiles.FirstOrDefault()?.Path ?? CWD?.Path;
        if (path == null)
            return;

        if (SelectedFiles.Count == 1
            && SelectedFiles.FirstOrDefault() is FileExplorerItemDTO item
            && !item.IsFile)
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
            };

            bool? result = await DialogService.ShowMessageBox(
                Loc["Get Details"],
                Loc["Are you sure you want to get this file details?"],
                yesText: Loc["Get Details"], cancelText: Loc["CancelChanges"], options: options);

            if (result != true)
                return;
        }

        var _options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
        };

        var parameters = new DialogParameters()
        {
            ["Files"] = SelectedFiles,
            ["Usernames"] = Usernames,
            ["Url"] = Url,
            ["AccountName"] = AccountName,
            ["ContainerName"] = ContainerName,
        };
        var dialogRef = await DialogService.ShowAsync<DetailDialog>("", parameters, _options);
    }

    private async Task<List<UserDetails>> GetUsers(List<string> userIds)
    {
        try
        {
            var filter = new ODataFilterGenerator()
                .Add(nameof(UserDetails.ID), ODataOperator.In, userIds)
                .ToString();
            var url = SettingManager.Configuration.UserListEndpoint + "?$filter=" + filter;

            var users = await HttpClient.GetFromJsonAsync<ODataDTO<UserDetails>>(url);
            return users?.Value ?? [];
        }
        catch (Exception)
        {
            return [];
        }
        
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

    public void SetView(FileView? view = null, bool save = true)
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

        if (save)
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

    private string SpecialItemClasses(FileExplorerItemDTO file)
    {
        var classes = new List<string>()
        {
            "file-explorer-item",
        };
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

    private void DisplayError(string message)
    {
        Snackbar.Add(message ?? Loc["Could not parse server data"], severity: Severity.Error, configure: o =>
        {
            o.VisibleStateDuration = 5000;
        });
    }

    private async Task FileUploaderValuesChanged(List<ShiftFileDTO> files)
    {
        if (files?.Any(x => (x.Data is FileUploadState state && state == FileUploadState.Uploaded)) == true)
        {
            await Refresh();
        }
        else
        {
            DisplayError(Loc["Uploading Failed."]);
        }
    }

    public void LocalSearch(string q)
    {
        var filtered = Files.Where(x => x.Name?.Contains(q, StringComparison.CurrentCultureIgnoreCase) == true);
        SearchQuery = q;

        if (q.Length > 0)
        {
            FilteredFiles = filtered.ToList();
        }
        else
        {
            FilteredFiles = null;
        }
    }

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        FilteredFiles = null;
    }

    private async Task LoadMore()
    {
        if (CWD == null)
            return;

        var data = new FileExplorerReadDTO
        {
            Path = CWD.Path,
            AccountName = AccountName,
            ContainerName = ContainerName,
            IncludeDeleted = ShowDeletedFiles,
            ContinuationToken = CWD.ContinuationToken,
        };

        await FetchData(data);
    }

    public void Dispose()
    {
        IShortcutComponent.Remove(Id);
        NavigationManager.LocationChanged -= LocationChanged;
    }
}
