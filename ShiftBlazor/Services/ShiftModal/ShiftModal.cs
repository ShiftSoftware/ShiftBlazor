using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Events;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ShiftModal
    {
        private readonly IJSRuntime JsRuntime;
        private readonly NavigationManager NavManager;
        private readonly IDialogService DialogService;
        private readonly SettingManager SettingManager;
        private readonly MessageService MessageService;

        private static readonly string QueryKey = "modal";
        private readonly List<Assembly> Assemblies;

        public ShiftModal(IJSRuntime jsRuntime, NavigationManager navManager, IDialogService dialogService, SettingManager settingManager, MessageService messageService)
        {
            JsRuntime = jsRuntime;
            NavManager = navManager;
            DialogService = dialogService;
            SettingManager = settingManager;
            MessageService = messageService;

            Assemblies = [Assembly.GetEntryAssembly()];

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
        public async Task<DialogResult?> Open<TComponent>(object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null, bool skipQueryParamUpdate = false) where TComponent : ComponentBase
        {
            var ComponentType = typeof(TComponent);
            return await Open(ComponentType, key, openMode, parameters, skipQueryParamUpdate);
        }

        public async Task<DialogResult?> Open(string ComponentPath, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null, bool skipQueryParamUpdate = false)
        {
            var ComponentType = GetComponentType(ComponentPath);
            if (ComponentType != null)
            {
                return await Open(ComponentType, key, openMode, parameters, skipQueryParamUpdate);
            }
            else
            {
                return DialogResult.Cancel();
            }
        }

        public async Task<DialogResult?> Open(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null, bool skipQueryParamUpdate = false)
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
                    if (ComponentType.FullName?.Contains(assemblyName) == true)
                    {
                        var fullname = ComponentType.FullName.Substring(assemblyName.Length + 1);
                        var queryParams = skipQueryParamUpdate ? null : parameters;
                        await UpdateModalQueryUrl(fullname, key, queryParams);
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
        public static string GenerateQueryString(in Dictionary<string, object>? parameters)
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
        public void Close(IMudDialogInstance mudDialog, object? data = null)
        {
            ShiftBlazorEvents.TriggerOnModalClosed(data);

            _ = RemoveFrontModalFromUrl();
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
        public async Task UpdateModals()
        {
            var (success, url) = await JsRuntime.InvokeAsyncWithErrorHandling<string>("GetUrl");

            if (!success) return;

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
            _ = UpdateModalQueryUrl(null, key);
        }

        private async Task UpdateModalQueryUrl(string? name, object? key, Dictionary<string, object>? parameters = null)
        {
            var (success, url) = await JsRuntime.InvokeAsyncWithErrorHandling<string>("GetUrl");

            if (!success) return;

            var modals = ParseModalUrl(url);

            if (name == null)
            {
                var modal = modals.LastOrDefault();
                if (modal != null && (modal.Key == null || modal.Key != key))
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

        private async Task<DialogResult?> OpenDialog(Type TComponent, object? key = null, Dictionary<string, object>? parameters = null)
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

            var options = new DialogOptions { NoHeader = true, BackgroundClass = "shift-modal-background", CloseOnEscapeKey = false };
            var dialogRef = await DialogService.ShowAsync(TComponent, "", dParams, options);
            var result = await dialogRef.Result;
            return result;
        }

        public List<ModalInfo> ParseModalUrl(string url)
        {
            var uri = NavManager.ToAbsoluteUri(url);

            var modalString = HttpUtility.ParseQueryString(uri.Query).Get(QueryKey);

            if (modalString != null)
            {
                var decodedString = Uri.UnescapeDataString(modalString);
                try
                {
                    return JsonSerializer.Deserialize<List<ModalInfo>>(decodedString) ?? [];
                }
                catch (Exception) { }
            }

            return [];
        }

        private string CreateModalUrlQuery(List<ModalInfo> modals, string url)
        {
            var queryString = "";

            if (modals.Count > 0)
            {
                try
                {
                    var modalString = JsonSerializer.Serialize(modals);
                    var param = new Dictionary<string, object?>
                    {
                        {QueryKey, Uri.EscapeDataString(modalString) },
                    };

                    var queryValues = param.Select(x => x.Key + "=" + x.Value);
                    queryString = "?" + string.Join("&", queryValues);
                }
                catch (Exception e)
                {
                    MessageService.Error("Could not create form url", e.Message, e.ToString());
                }
            }

            var uri = NavManager.ToAbsoluteUri(url);
            return uri.AbsolutePath + queryString;
        }

        private async Task RemoveFrontModalFromUrl()
        {
            var (success, url) = await JsRuntime.InvokeAsyncWithErrorHandling<string>("GetUrl");

            if (!success) return;

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
