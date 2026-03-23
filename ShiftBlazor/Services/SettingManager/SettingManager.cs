using BitzArt.Blazor.Cookies;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Globalization;
using System.Net.Http.Headers;

namespace ShiftSoftware.ShiftBlazor.Services;

public class SettingManager
{
    private enum StorageType {
        Cookie,
        LocalStorage
    }

    private readonly NavigationManager NavManager;
    private readonly HttpClient Http;
    private readonly ICookieService CookieService;
    private readonly ILocalStorageService LocalStorageService;

    private readonly string Key = "ShiftSettings";

    public AppSetting Settings { get => field; internal set => field = value; } = new();
    public LanguageInfo SelectedLanguage { get => field; internal set => field = value; }
    public Dictionary<string, FileExplorerSettings>? FileExplorer { get => field; internal set => field = value; }
    public Dictionary<string, DataGridSettings>? DataGrid { get => field; internal set => field = value; }

    public DeviceInfo Device { get => field; internal set => field = value; } = new();
    public static AppConfiguration Configuration { get => field; internal set => field = value; } = new();

    private bool Initialized = false;

    public SettingManager(ICookieService cookieService,
                          ILocalStorageService localStorageService,
                          NavigationManager navManager,
                          HttpClient http,
                          IOptions<AppConfiguration> config)
    {
        CookieService = cookieService;
        LocalStorageService = localStorageService;
        NavManager = navManager;
        Http = http;
        Configuration = config.Value;

        if (string.IsNullOrWhiteSpace(Configuration.BaseAddress))
            throw new ArgumentNullException(nameof(Configuration.BaseAddress));

        SelectedLanguage = Configuration.Languages.FirstOrDefault() ?? DefaultAppSetting.Language;
    }

    #region helper functions

    public async Task Setup(bool wasm)
    {
        if (Initialized)
            return;

        var settings = await CookieService.GetValueAsync<AppSetting>(Key);
        var explorerSettings = await CookieService.GetValueAsync<Dictionary<string, FileExplorerSettings>>($"{Key}-{nameof(FileExplorer)}");
        var datagridSettings = await CookieService.GetValueAsync<Dictionary<string, DataGridSettings>>($"{Key}-{nameof(DataGrid)}");

        var cultureCookie = await CookieService.GetAsync(CookieRequestCultureProvider.DefaultCookieName);
        var cultures = cultureCookie?.Value;

        var cultureName = cultures != null
            ? CookieRequestCultureProvider.ParseCookieValue(cultures).Cultures.FirstOrDefault().ToString()
            : null;

        Settings = settings ?? new AppSetting();
        FileExplorer = explorerSettings ?? new();
        DataGrid = datagridSettings ?? new();
        SelectedLanguage = Configuration.Languages.FirstOrDefault(l => l.CultureName == cultureName) ?? DefaultAppSetting.Language;

        if (wasm)
        {
            UpdateCulture(cultureName);

            //FileExplorer = await LocalStorageService.GetItemAsync<Dictionary<string, FileExplorerSettings>>($"{Key}-{nameof(FileExplorer)}") ?? new();
            //DataGrid = await LocalStorageService.GetItemAsync<Dictionary<string, DataGridSettings>>($"{Key}-{nameof(DataGrid)}") ?? new();
        }

        Initialized = true;
    }

    private void SetItem<T>(string key, T value, StorageType storage = StorageType.LocalStorage)
    {
        if (storage == StorageType.Cookie)
        {
            var valueString = System.Text.Json.JsonSerializer.Serialize(value);
            var base64Value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(valueString));
            CookieService.SetAsync(key, base64Value, expiration: DateTime.Now.AddYears(5), httpOnly: false, secure: false, SameSiteMode.Strict);
        }
        else if (storage == StorageType.LocalStorage)
        {
            LocalStorageService.SetItemAsync(key, value);
        }
    }

    public CultureInfo GetCulture(string? cultureName = null)
    {
        cultureName ??= GetLanguage().CultureName;
        return new CultureInfo(cultureName)
        {
            DateTimeFormat = new DateTimeFormatInfo
            {
                LongDatePattern = GetDateFormat(),
                ShortDatePattern = GetDateFormat(),

                LongTimePattern = GetTimeFormat(),
                ShortTimePattern = GetTimeFormat(),
            },
            NumberFormat = new NumberFormatInfo
            {
                NativeDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"],
            },
        };
    }

    private void UpdateCulture(string? cultureName = null)
    {
        var culture = GetCulture(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        ShiftSoftware.ShiftEntity.Model.LocalizedTextJsonConverter.UserLanguage = culture.TwoLetterISOLanguageName;

        Http.DefaultRequestHeaders.AcceptLanguage.Clear();
        Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
    }

    private async Task<DataGridSettings> GetDataGridSettings(string id)
    {
        if (DataGrid == null)
        {
            DataGrid = await LocalStorageService.GetItemAsync<Dictionary<string, DataGridSettings>>($"{Key}-{nameof(DataGrid)}");
        }

        var datagrid = DataGrid?.GetValueOrDefault(id);

        if (datagrid == null)
        {
            datagrid = new DataGridSettings();
            DataGrid?.Add(id, datagrid);
        }

        return datagrid;
    }

    private async Task<FileExplorerSettings> GetFileExplorerSettings(string id)
    {
        if (FileExplorer == null)
        {
            FileExplorer = await LocalStorageService.GetItemAsync<Dictionary<string, FileExplorerSettings>>($"{Key}-{nameof(FileExplorer)}") ?? [];
        }

        var fileExplorer = FileExplorer.GetValueOrDefault(id);

        if (fileExplorer == null)
        {
            fileExplorer = new FileExplorerSettings();
            FileExplorer?.Add(id, fileExplorer);
        }

        return fileExplorer;
    }

    #endregion

    public void SwitchLanguage(LanguageInfo lang, bool forceReload = true)
    {
        SelectedLanguage = lang;
        var newCulture = GetCulture(lang.CultureName);

        if (forceReload)
            SetCulture(newCulture);
    }

    public void SetCulture(CultureInfo culture)
    {
        var uri = new Uri(NavManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
        var cultureEscaped = Uri.EscapeDataString(culture.Name);
        var uriEscaped = Uri.EscapeDataString(uri);

        NavManager.NavigateTo(
            $"api/Culture/Set?culture={cultureEscaped}&redirectUri={uriEscaped}",
            forceLoad: true);
    }

    public LanguageInfo GetLanguage()
    {
        return SelectedLanguage;
    }

    #region general settings

    public void SetDateFormat(string format)
    {
        if (Settings.DateFormat != format)
        {
            Settings.DateFormat = format;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }

    public void SetTimeFormat(string format)
    {
        if (Settings.TimeFormat != format)
        {
            Settings.TimeFormat = format;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }

    public string GetDateFormat()
    {
        return Settings.DateFormat ?? DefaultAppSetting.DateFormat;
    }

    public string GetTimeFormat()
    {
        return Settings.TimeFormat ?? DefaultAppSetting.TimeFormat;
    }
    
    public void SetModalPosition(DialogPosition position)
    {
        if (Settings.ModalPosition != position)
        {
            Settings.ModalPosition = position;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }

    public DialogPosition GetModalPosition()
    {
        return Settings.ModalPosition ?? DefaultAppSetting.ModalPosition;
    }

    public void SetModalWidth(MaxWidth width)
    {
        if (Settings.ModalWidth != width)
        {
            Settings.ModalWidth = width;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }

    public MaxWidth GetModalWidth()
    {
        return Settings.ModalWidth ?? DefaultAppSetting.ModalWidth;
    }

    public void SetFormSaveAction(FormOnSaveAction action)
    {
        if (Settings.FormOnSaveAction != action)
        {
            Settings.FormOnSaveAction = action;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }
    public FormOnSaveAction GetFormOnSaveAction()
    {
        return Settings.FormOnSaveAction ?? DefaultAppSetting.FormOnSaveAction;
    }

    public void SetFormCloneSetting(bool enableClone)
    {
        Settings.EnableFormClone = enableClone;
        SetItem(Key, Settings, StorageType.Cookie);
    }

    public bool GetFormCloneSetting()
    {
        return Settings.EnableFormClone ?? DefaultAppSetting.EnableFormClone;
    }

    private AppSetting GetSettings()
    {
        return Settings;
    }

    public bool SetDrawerState(bool open)
    {
        if (Settings.IsDrawerOpen != open)
        {
            Settings.IsDrawerOpen = open;
            SetItem(Key, Settings, StorageType.Cookie);
        }

        return GetDrawerState();
    }

    public bool GetDrawerState()
    {
        return Settings.IsDrawerOpen ?? DefaultAppSetting.IsDrawerOpen;
    }

    public void SetListPageSize(int size)
    {
        if (Settings.GlobalListPageSize != size)
        {
            Settings.GlobalListPageSize = size;
            SetItem(Key, Settings, StorageType.Cookie);
        }
    }
    public int GetListPageSize()
    {
        return Settings.GlobalListPageSize ?? DefaultAppSetting.ListPageSize;
    }

    #endregion

    #region settings in localstorage

    public void SetFileExplorerSetting(string id, FileExplorerSettings setting)
    {
        var fileExplorer = setting;
        SetItem($"{Key}-{nameof(FileExplorer)}", FileExplorer, StorageType.Cookie);
    }

    public async Task<FileExplorerSettings> GetFileExplorerSetting(string id)
    {
        var fileExplorer = await GetFileExplorerSettings(id);
        return fileExplorer;
    }

    public async Task SetColumnState(string id, List<ColumnState> columnNames)
    {
        var datagrid = await GetDataGridSettings(id);

        datagrid.ColumnStates = columnNames;
        SetItem($"{Key}-{nameof(DataGrid)}", DataGrid, StorageType.Cookie);
    }

    public async Task<List<ColumnState>> GetColumnState(string id)
    {
        var datagrid = await GetDataGridSettings(id);
        return datagrid?.ColumnStates ?? [];
    }

    public async Task SetListPageSize(string id, int size)
    {
        var datagrid = await GetDataGridSettings(id);

        datagrid.PageSize = size;
        SetItem($"{Key}-{nameof(DataGrid)}", DataGrid, StorageType.Cookie);
    }

    public async Task<int> GetListPageSize(string id, int? defaultSize = null)
    {
        var datagrid = await GetDataGridSettings(id);

        return datagrid?.PageSize
            ?? defaultSize
            ?? Settings.GlobalListPageSize
            ?? DefaultAppSetting.ListPageSize;
    }

    public async Task<bool> SetFilterPanelState(string id, bool open)
    {
        var datagrid = await GetDataGridSettings(id);

        datagrid.IsFilterPanelOpen = open;
        SetItem($"{Key}-{nameof(DataGrid)}", DataGrid, StorageType.Cookie);

        return open;
    }

    public async Task<bool> GetFilterPanelState(string id, bool? defaultState = null)
    {
        var datagrid = await GetDataGridSettings(id);
        return datagrid?.IsFilterPanelOpen
            ?? defaultState
            ?? DefaultAppSetting.IsDataGridFilterPanelOpen;
    }

    #endregion

    private async Task<DeviceInfo> GetDeviceInfo()
    {
        throw new NotImplementedException();
    }
}