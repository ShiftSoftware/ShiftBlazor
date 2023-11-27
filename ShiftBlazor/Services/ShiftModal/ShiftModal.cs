using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ShiftModal
    {
        private readonly IJSRuntime JsRuntime;
        private readonly NavigationManager NavManager;
        private readonly IDialogService DialogService;
        private readonly SettingManager SettingManager;

        private static readonly string QueryKey = "modal";
        private readonly List<Assembly> Assemblies;

        public ShiftModal(IJSRuntime jsRuntime, NavigationManager navManager, IDialogService dialogService, SettingManager settingManager)
        {
            JsRuntime = jsRuntime;
            NavManager = navManager;
            DialogService = dialogService;
            SettingManager = settingManager;

            Assemblies = new List<Assembly> { Assembly.GetEntryAssembly()! };

            if (SettingManager.Configuration.AdditionalAssemblies != null)
            {
                Assemblies.AddRange(SettingManager.Configuration.AdditionalAssemblies);
            }

        }


        /// <summary>
        ///     Open a form modal or page.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to open inside the form</typeparam>
        /// <param name="key">The ID of the item being View or edited</param>
        /// <param name="openMode">Popup, Redirect, NewTab</param>
        /// <param name="parameters">Additional component parameters to send with the form</param>
        /// <returns>DialogResult</returns>
        public async Task<DialogResult?> Open<TComponent>(object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null) where TComponent : ComponentBase
        {
            var ComponentType = typeof(TComponent);
            return await Open(ComponentType, key, openMode, parameters);
        }

        public async Task<DialogResult?> Open(string ComponentPath, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            var ComponentType = GetComponentType(ComponentPath);
            if (ComponentType != null)
            {
                return await Open(ComponentType, key, openMode, parameters);
            }
            else
            {
                return DialogResult.Cancel();
            }
        }

        public async Task<DialogResult?> Open(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            if (ComponentType.IsAssignableFrom(typeof(ComponentBase)))
            {
                throw new Exception("ShiftModal: Object is not a component");
            }

            if (openMode == ModalOpenMode.NewTab)
            {
                await JsRuntime.InvokeVoidAsync("open", $"{ComponentType.Name}/{key}{GenerateQueryString(parameters)}", "_blank");
            }
            else if (openMode == ModalOpenMode.Redirect)
            {
                NavManager.NavigateTo($"{ComponentType.Name}/{key}{GenerateQueryString(parameters)}");
            }
            else if (openMode == ModalOpenMode.Drawer_Start)
            {

            }
            else if (openMode == ModalOpenMode.Drawer_End)
            {

            }
            else if (openMode == ModalOpenMode.Popup)
            {
                foreach (var assembly in Assemblies)
                {
                    var assemblyName = assembly.GetName().Name!;
                    if (ComponentType!.FullName!.Contains(assemblyName))
                    {
                        var fullname = ComponentType!.FullName!.Substring(assemblyName.Length + 1);
                        UpdateModalQueryUrl(fullname, key, parameters);
                    }
                }
                
                return await OpenDialog(ComponentType, key, parameters);
            }

            return null;
        }

        /// <summary>
        ///     Generate query string from a Dictionary without the URL.
        /// </summary>
        /// <param name="parameters">A Dictionary of query options</param>
        /// <returns>A query string that start with '?'</returns>
        public string GenerateQueryString(Dictionary<string, string>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return string.Empty;

            var queries = parameters.Select(x => $"{x.Key}={x.Value}");

            return "?" + string.Join("&", queries);
        }

        /// <summary>
        ///     Close the form dialog.
        /// </summary>
        /// <param name="mudDialog">An instance of the MudDialog.</param>
        public void Close(MudDialogInstance mudDialog, object? data = null)
        {
            RemoveFrontModalFromUrl();
            if (data == null)
            {
                mudDialog.Cancel();
            }
            else
            {
                mudDialog.Close(data);
            }
        }

        /// <summary>
        ///     Checks whether the url has any modal info in the URL query, if found, open them.
        /// </summary>
        public async void UpdateModals()
        {
            var url = await JsRuntime.InvokeAsync<string>("GetUrl");
            var modals = ParseModalUrl(url);

            if (modals.Count == 0)
            {
                return;
            }

            foreach (var modal in modals)
            {
                var type = GetComponentType(modal.Name);
                if (type != null)
                {
                    _ = OpenDialog(type, modal.Key, modal.Parameters);
                }
            }
        }

        /// <summary>
        ///     Update the URL when the ID of an item changes or is created.
        /// </summary>
        /// <param name="key"></param>
        public void UpdateKey(object? key)
        {
            UpdateModalQueryUrl(null, key);
        }

        private async void UpdateModalQueryUrl(string? name, object? key, Dictionary<string, string>? parameters = null)
        {
            var url = await JsRuntime.InvokeAsync<string>("GetUrl");
            var modals = ParseModalUrl(url);

            if (name == null)
            {
                var modal = modals.LastOrDefault();
                if (modal != null && modal.Key == null)
                {
                    modal.Key = key;
                    modal.Parameters = parameters;
                }
            }
            else
            {
                modals.Add(new ModalInfo { Name = name, Key = key, Parameters = parameters });
            }

            var newUrl = CreateModalUrlQuery(modals, url);
            await JsRuntime.InvokeVoidAsync("history.pushState", null, "", newUrl);
        }

        internal Type? GetComponentType(string name)
        {
            foreach (var assembly in Assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                var compName = $"{assemblyName}.{name}";
                var type = assembly.GetType(compName);

                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private async Task<DialogResult> OpenDialog(Type TComponent, object? key = null, Dictionary<string, string>? parameters = null)
        {
            var dParams = new DialogParameters();

            if (key != null)
            {
                dParams.Add("Key", key);
            }

            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    dParams.Add(item.Key, item.Value);
                }
            }

            var options = new DialogOptions { NoHeader = true, ClassBackground = "shift-modal-background" };
            var result = await DialogService.Show(TComponent, "", dParams, options).Result;
            return result;
        }

        public List<ModalInfo> ParseModalUrl(string url)
        {
            var uri = NavManager.ToAbsoluteUri(url);

            var modalString = HttpUtility.ParseQueryString(uri.Query).Get(QueryKey);

            if (modalString != null)
            {
                var decodedString = WebUtility.UrlDecode(modalString);
                try
                {
                    return JsonSerializer.Deserialize<List<ModalInfo>>(decodedString) ?? new List<ModalInfo>();
                }
                catch (Exception) { }
            }

            return new List<ModalInfo>();
        }

        private string CreateModalUrlQuery(List<ModalInfo> modals, string url)
        {
            var queryString = "";

            if (modals.Count > 0)
            {
                var modalString = JsonSerializer.Serialize(modals);
                var param = new Dictionary<string, object?>
                {
                    //{QueryKey, System.Net.WebUtility.UrlEncode(modalString) },
                    {QueryKey, modalString },
                };

                var queryValues = param.Select(x => x.Key + "=" + x.Value);
                queryString = "?" + string.Join("&", queryValues);
            }

            var uri = NavManager.ToAbsoluteUri(url);
            return uri.AbsolutePath + queryString;
        }

        private async void RemoveFrontModalFromUrl()
        {
            var url = await JsRuntime.InvokeAsync<string>("GetUrl");

            var modals = ParseModalUrl(url);
            if (modals.Count > 0)
            {
                modals.RemoveAt(modals.Count - 1);
            }
            var newUrl = CreateModalUrlQuery(modals, url);
            await JsRuntime.InvokeVoidAsync("history.pushState", null, "", newUrl);
        }
    }
}
