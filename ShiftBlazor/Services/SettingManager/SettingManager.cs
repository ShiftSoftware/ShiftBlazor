using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;
using System.Net.Http.Headers;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class SettingManager
    {
        private readonly ISyncLocalStorageService SyncLocalStorage;
        private readonly NavigationManager? NavManager;
        private readonly HttpClient? Http;

        private readonly string Key = "ShiftSettings";
        public AppSetting Settings { get; set; }
        public AppConfiguration Configuration { get; set; } = new();
        private string DefaultCultureName = "en-US";

        public SettingManager(ISyncLocalStorageService syncLocalStorage,
                              NavigationManager? navManager,
                              HttpClient? http,
                              Action<AppConfiguration> config)
        {
            SyncLocalStorage = syncLocalStorage;
            NavManager = navManager;
            Http = http;
            config.Invoke(Configuration);

            if (string.IsNullOrWhiteSpace(Configuration.BaseAddress)) throw new ArgumentNullException(nameof(Configuration.BaseAddress));

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

        public void SetListPageSize(int? size)
        {
            Settings.ListPageSize = size;
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public void SetModalPosition(DialogPosition position)
        {
            Settings.ModalPosition = position;
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public CultureInfo GetCulture()
        {
            return new CultureInfo(Settings.CurrentLanguage?.CultureName ?? DefaultCultureName)
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
                SyncLocalStorage.SetItem(Key, settings);
            }

            return settings;
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