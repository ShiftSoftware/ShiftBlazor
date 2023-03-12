using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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
        public ShiftBlazorSettings Settings { get; set; }

        public SettingManager(ISyncLocalStorageService syncLocalStorage, NavigationManager? navManager, HttpClient? http)
        {
            SyncLocalStorage = syncLocalStorage;
            NavManager = navManager;
            Http = http;

            Settings = GetSettings();

            UpdateCulture();
        }

        public void SwitchLanguage(string name, bool forceReload = true)
        {
            Settings.CultureName = name;

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

        public void SetModalPosition(MudBlazor.DialogPosition position)
        {
            Settings.ModalPosition = position;
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public CultureInfo GetCulture()
        {
            return new CultureInfo(Settings.CultureName)
            {
                DateTimeFormat = new DateTimeFormatInfo()
                {
                    ShortDatePattern = Settings.DateTimeFormat,
                },
            };
        }

        private ShiftBlazorSettings GetSettings()
        {
            ShiftBlazorSettings? settings = null;

            try
            {
                settings = SyncLocalStorage.GetItem<ShiftBlazorSettings>(Key);
            }
            catch { }

            if (settings == null)
            {
                settings = new ShiftBlazorSettings();
                SyncLocalStorage.SetItem(Key, settings);
            }

            return settings;
        }

        private void UpdateCulture()
        {
            var culture = GetCulture();

            //CultureInfo.DefaultThreadCurrentCulture = culture;
            //CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));
            }
        }

        public class ShiftBlazorSettings
        {
            public string CultureName { get; set; } = "en-US";
            public int? ListPageSize { get; set; }
            public MudBlazor.DialogPosition ModalPosition { get; set; } = MudBlazor.DialogPosition.Center;
            public string DateTimeFormat = "yyyy-MM-dd";

        }
    }
}
