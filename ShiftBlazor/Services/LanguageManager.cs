using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net.Http.Headers;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class LanguageManager
    {
        private readonly ISyncLocalStorageService SyncLocalStorage;
        private readonly NavigationManager? NavManager;
        private readonly HttpClient? Http;

        private readonly string Key = "BlazorCulture";
        private readonly string DefaultCultureName = "en-US";

        public LanguageManager(ISyncLocalStorageService syncLocalStorage, NavigationManager? navManager, HttpClient? http)
        {
            SyncLocalStorage = syncLocalStorage;
            NavManager = navManager;
            Http = http;

            string? cultureName = null;

            try
            {
                cultureName = syncLocalStorage.GetItem<string>(Key);
            }
            catch { }

            if (cultureName == null)
            {
                cultureName = DefaultCultureName;
                syncLocalStorage.SetItem(Key, cultureName);
            }

            UpdateCulture(cultureName);
        }

        public void SwitchLanguage(string name)
        {
            SyncLocalStorage.SetItem(Key, name);

            UpdateCulture(name);

            NavManager?.NavigateTo(NavManager.Uri, forceLoad: true);
        }

        private void UpdateCulture(string name)
        {
            CultureInfo culture = new CultureInfo(name)
            {
                DateTimeFormat = new DateTimeFormatInfo()
                {
                    ShortDatePattern = "yyyy-MM-dd"
                },
            };

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (Http != null)
            {
                Http.DefaultRequestHeaders.AcceptLanguage.Clear();
                Http.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(name));
            }
        }
    }
}
