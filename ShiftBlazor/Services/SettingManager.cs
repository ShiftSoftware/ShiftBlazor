using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Extensions;
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
        public ShiftBlazorConfiguration Configuration { get; set; } = new ShiftBlazorConfiguration();

        public SettingManager(ISyncLocalStorageService syncLocalStorage, NavigationManager? navManager, HttpClient? http, Action<ShiftBlazorConfiguration> config)
        {
            SyncLocalStorage = syncLocalStorage;
            NavManager = navManager;
            Http = http;
            config.Invoke(Configuration);

            if (string.IsNullOrWhiteSpace(Configuration.BaseAddress)) throw new ArgumentNullException(nameof(Configuration.BaseAddress));
            if (string.IsNullOrWhiteSpace(Configuration.UserListEndpoint)) throw new ArgumentNullException(nameof(Configuration.UserListEndpoint));

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

        public void SetModalPosition(DialogPosition position)
        {
            Settings.ModalPosition = position;
            SyncLocalStorage.SetItem(Key, Settings);
        }

        public CultureInfo GetCulture()
        {
            return new CultureInfo(Settings.CultureName)
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
            public string DateTimeFormat = "yyyy-MM-dd";
            public string CultureName { get; set; } = "en-US";
            public int? ListPageSize { get; set; }
            public DialogPosition ModalPosition { get; set; } = DialogPosition.Center;
        }

        public class ShiftBlazorConfiguration
        {
            public string BaseAddress { get; set; } = "";
            public string UserListEndpoint { get; set; } = "";

            private string _ApiPath = "/api";
            private string _ODataPath = "/odata";
            public string ApiPath
            {
                get => BaseAddress.AddUrlPath(_ApiPath);
                set => _ApiPath = value;
            }

            public string ODataPath
            {
                get => BaseAddress.AddUrlPath(_ODataPath);
                set => _ODataPath = value;
            }
        }
    }
}