using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Globalization;
using System.Net.Http.Headers;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class SettingManager
    {
        private readonly ISyncLocalStorageService SyncLocalStorage;
        private readonly NavigationManager? NavManager;
        private readonly HttpClient? Http;
        private readonly IJSRuntime? JsRuntime;

        private readonly string Key = "ShiftSettings";
        public AppSetting Settings { get; set; }
        public DeviceInfo Device { get; set; } = new DeviceInfo();
        public AppConfiguration Configuration { get; set; } = new();

        public SettingManager(ISyncLocalStorageService syncLocalStorage,
                              NavigationManager? navManager,
                              HttpClient? http,
                              IJSRuntime? jsRuntime,
                              Action<AppConfiguration> config)
        {
            SyncLocalStorage = syncLocalStorage;
            NavManager = navManager;
            Http = http;
            JsRuntime = jsRuntime;
            config.Invoke(Configuration);

            if (string.IsNullOrWhiteSpace(Configuration.BaseAddress)) throw new ArgumentNullException(nameof(Configuration.BaseAddress));

            GetDeviceInfo().ContinueWith(async x =>
            {
                Device = await x ?? new();
            });

            Settings = GetSettings();

            UpdateCulture();
        }

        public void SetDateFormat(string format)
        {
            if (Settings.DateFormat != format)
            {
                Settings.DateFormat = format;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public void SetTimeFormat(string format)
        {
            if (Settings.TimeFormat != format)
            {
                Settings.TimeFormat = format;
                SyncLocalStorage.SetItem(Key, Settings);
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

        public void SetListPageSize(int size)
        {
            if (Settings.ListPageSize != size)
            {
                Settings.ListPageSize = size;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }
        public int GetListPageSize()
        {
            return Settings.ListPageSize ?? DefaultAppSetting.ListPageSize;
        }
        
        public void SetModalPosition(DialogPosition position)
        {
            if (Settings.ModalPosition != position)
            {
                Settings.ModalPosition = position;
                SyncLocalStorage.SetItem(Key, Settings);
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
                SyncLocalStorage.SetItem(Key, Settings);
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
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }
        public FormOnSaveAction GetFormOnSaveAction()
        {
            return Settings.FormOnSaveAction ?? DefaultAppSetting.FormOnSaveAction;
        }
        
        public void SetColumnState(string id, List<ColumnState> columnNames)
        {
            if (Settings.ColumnStates == null)
            {
                Settings.ColumnStates = [];
            }

            Settings.ColumnStates.Remove(id);
            Settings.ColumnStates.Add(id, columnNames);
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public List<ColumnState> GetColumnState(string id)
        {
            return Settings.ColumnStates?.GetValueOrDefault(id) ?? [];
        }

        public void SwitchLanguage(LanguageInfo lang, bool forceReload = true)
        {
            Settings.Language = lang;

            SyncLocalStorage.SetItem(Key, Settings);

            UpdateCulture();

            if (forceReload)
            {
                NavManager?.NavigateTo(NavManager.Uri, forceLoad: true);
            }
        }
        public LanguageInfo GetLanguage()
        {
            return Settings.Language ?? DefaultAppSetting.Language;
        }

        public void SetFormCloneSetting(bool enableClone)
        {
            Settings.EnableFormClone = enableClone;
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public bool GetFormCloneSetting()
        {
            return Settings.EnableFormClone ?? DefaultAppSetting.EnableFormClone;
        }

        public CultureInfo GetCulture()
        {
            return new CultureInfo(GetLanguage().CultureName)
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
                    NativeDigits = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" },
                },
            };
        }

        private AppSetting GetSettings()
        {
            AppSetting? settings = null;

            try
            {
                settings = SyncLocalStorage.GetItem<AppSetting>(Key);
            }
            catch { }

            if (settings == null)
            {
                settings = new AppSetting();
                SyncLocalStorage.SetItem(Key, settings);
            }

            return settings;
        }

        private async Task<DeviceInfo> GetDeviceInfo()
        {
            return await JsRuntime!.InvokeAsync<DeviceInfo>("getWindowDimensions");
        }

        private void UpdateCulture()
        {
            var culture = GetCulture();

            ShiftSoftware.ShiftEntity.Model.LocalizedTextJsonConverter.UserLanguage = culture.TwoLetterISOLanguageName;

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
            }
        }

    }
}