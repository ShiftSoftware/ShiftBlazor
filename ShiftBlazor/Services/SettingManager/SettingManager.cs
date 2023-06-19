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
                Device = await x;
            });

            Settings = GetSettings();

            UpdateCulture();
        }

        public void SetDateTimeFormat(string format)
        {
            if (Settings.DateTimeFormat != format)
            {
                Settings.DateTimeFormat = format;
                SyncLocalStorage.SetItem(Key, Settings);
            }
        }
        public string GetDateTimeFormat()
        {
            return Settings.DateTimeFormat ?? DefaultAppSetting.DateTimeFormat;
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
        
        public void SetHiddenColumns(string id, List<string> columnNames)
        {
            if (Settings.HiddenColumns == null)
            {
                Settings.HiddenColumns = new();
            }
            Settings.HiddenColumns.Remove(id);
            Settings.HiddenColumns.Add(id, columnNames);
            SyncLocalStorage.SetItem(Key, Settings);
        }
        public List<string> GetHiddenColumns(string id)
        {
            return Settings.HiddenColumns?.GetValueOrDefault(id) ?? new();
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

        public CultureInfo GetCulture()
        {
            return new CultureInfo(GetLanguage().CultureName)
            {
                DateTimeFormat = new DateTimeFormatInfo
                {
                    ShortDatePattern = GetDateTimeFormat(),
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

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
            }
        }

    }
}