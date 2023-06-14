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
        public static LanguageInfo DefaultLanguage { get; set; } = new LanguageInfo
        {
            CultureName = "en-US",
            Label = "English",
            RTL = false,
        };

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
                Device = await x;
            });

            Settings = GetSettings();

            UpdateCulture();
        }

        public void SwitchLanguage(LanguageInfo lang, bool forceReload = true)
        {
            Settings.CurrentLanguage = lang;

            SyncLocalStorage.SetItem(Key, Settings);

            UpdateCulture();

            if (forceReload)
            {
                NavManager?.NavigateTo(NavManager.Uri, forceLoad: true);
            }
        }

        public void SetHiddenColumns(string id, List<string> columnNames)
        {
            Settings.HiddenColumns.Remove(id);
            Settings.HiddenColumns.Add(id, columnNames);
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public List<string> GetHiddenColumns(string id)
        {
            return Settings.HiddenColumns.GetValueOrDefault(id) ?? new();
        }

        public void SetListPageSize(int size)
        {
            if (Settings.ListPageSize != size)
            {
                Settings.ListPageSize = size;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public void SetModalPosition(DialogPosition position)
        {
            if (Settings.ModalPosition != position)
            {
                Settings.ModalPosition = position;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public void SetModalWidth(MaxWidth width)
        {
            if (Settings.ModalWidth != width)
            {
                Settings.ModalWidth = width;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public void SetDateTimeFormat(string format)
        {
            if (Settings.DateTimeFormat != format)
            {
                Settings.DateTimeFormat = format;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public void SetFormSaveAction(FormOnSaveAction action)
        {
            if (Settings.FormOnSaveAction != action)
            {
                Settings.FormOnSaveAction = action;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }

        public CultureInfo GetCulture()
        {
            return new CultureInfo(Settings.CurrentLanguage.CultureName)
            {
                DateTimeFormat = new DateTimeFormatInfo
                {
                    ShortDatePattern = Settings.DateTimeFormat,
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
                if (Configuration.Languages.Count > 0)
                {
                    settings.CurrentLanguage = Configuration.Languages.First();
                }
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

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
            }
        }

    }
}