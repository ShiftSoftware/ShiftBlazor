using Blazored.LocalStorage;
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
using System.Net.Http.Json;
using System.Text.Json;

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

    public bool IsEmbed { get; private set; } = false;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();

    private string FileExplorerId { get; set; }
    private string ToolbarStyle = string.Empty;
    private Size IconSize = Size.Medium;
    private bool DisableSidebar => DisableQuickAccess && DisableRecents;
    private FileExplorerDirectoryContent? CWD { get; set; } = null;
    private List<FileExplorerDirectoryContent> Files { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private string Url = "";
    private FileUploader? _FileUploader { get; set; }
    private List<FileExplorerDirectoryContent> SelectedFiles { get; set; } = [];
    private List<string> QuickAccessFiles { get; set; } = [];
    private List<string> PathParts = [];
    private FileExplorerDirectoryContent? LastSelectedFile { get; set; }
    private bool RenderQuickAccess => !DisableQuickAccess && QuickAccessFiles.Count > 0;
    private bool ShowDeletedFiles { get; set; }

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
        GetQuickAccessItems();
    }

    protected override async Task OnInitializedAsync()
    {
        await FetchData();
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {

        }
    }

    public async Task FetchData(FileExplorerDirectoryContent? data = null)
    {
        IsLoading = true;

        //await Task.Delay(1000);

        try
        {
            var response = await HttpClient.PostAsJsonAsync(Url, new FileExplorerDirectoryContent()
            {
                Action = "read",
                Path = data == null ? "/" : data.FilterPath + data.Name,
                Data = data == null ? [] : [data],
                ShowHiddenItems = ShowDeletedFiles,
            });

            if (!response.IsSuccessStatusCode)
            {
                //ErrorMessage = Loc["DataReadStatusError", (int)response!.StatusCode];
                //ReadyToRender = true;
                //return gridData;
            }

            var content = await response.Content.ReadFromJsonAsync<FileExplorerResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            //if (content == null || content.Count == null)
            //{
            //    ErrorMessage = Loc["DataReadEmptyError"];
            //    ReadyToRender = true;
            //    return gridData;
            //}

            Files = content?.Files?.ToList() ?? new List<FileExplorerDirectoryContent>();
            CWD = content?.CWD;
            var crumbPath = content.CWD.FilterPath == "" ? "" : content.CWD.FilterPath + content.CWD.Name;
            SetBreadcrumb(crumbPath);

            IsLoading = false;

        }
        catch (Exception e)
        {
            MessageService.Error("Could not parse server data.", e.Message, e.ToString());
        }

        StateHasChanged();
    }

    private void SetBreadcrumb(string path = "")
    {
        var breadcrumb = new List<string>
        {
            RootAliasName ?? "Root",
        };

        breadcrumb.AddRange(path.Split('/', StringSplitOptions.RemoveEmptyEntries));

        Console.WriteLine(JsonSerializer.Serialize(breadcrumb));
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

    }

    private void DeselectAllFiles(MouseEventArgs args)
    {
        SelectedFiles.Clear();
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

        var ac = DialogService.Show<CreateFolderDialog>("", options);
        var avb = await ac.Result;
        
        if (avb.Data is string value)
        {
            Console.WriteLine(value);

            var newFolderData = new FileExplorerDirectoryContent()
            {
                Action = "create",
                Path = CWD?.FilterPath + CWD?.Name,
                Name = value,
                Data = [],
            };

            var response = await HttpClient.PostAsJsonAsync(Url, newFolderData);
            await FetchData(CWD);
        }
    }

    private async Task Upload()
    {
        if (_FileUploader != null)
        {
            await _FileUploader.OpenInput(directoryUpload: false);
            Console.WriteLine($"{this.Root} + {CWD?.FilterPath} + {CWD?.Name}");
        }
    }

    private void Sort()
    {
        // Logic to sort files
    }

    private async Task Refresh(bool force = false)
    {
        if (force)
        {
            Files = [];
        }

        await FetchData(CWD);
    }

    private async Task Delete()
    {
        if (SelectedFiles == null)
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
            var files = SelectedFiles.ToArray();
            var newFolderData = new FileExplorerDirectoryContent()
            {
                Action = "delete",
                Path = SelectedFiles.First().FilterPath,
                Data = files,
            };

            var response = await HttpClient.PostAsJsonAsync(Url, newFolderData);
            await FetchData(CWD);
        }
    }

    private async Task Download(FileExplorerDirectoryContent? file = null)
    {
        if (file == null && SelectedFiles.Count > 0)
        {
            file = SelectedFiles.Last();
        }
        await JsRuntime.InvokeVoidAsync("downloadFileFromUrl", file.Name, file.TargetPath);
    }

    private void GetQuickAccessItems()
    {
        var items = SyncLocalStorage.GetItem<List<string>>("QuickAccess");

        QuickAccessFiles = items ?? [];
    }

    private void AddToQuickAccess()
    {
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
            switch(Path.GetExtension(file.Path))
            {
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

    void IDisposable.Dispose()
    {
        IShortcutComponent.Remove(Id);
    }
}
