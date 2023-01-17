﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Reflection;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ShiftModalService
    {
        private readonly IJSRuntime JsRuntime;
        private readonly NavigationManager NavManager;
        private readonly IDialogService DialogService;

        private static readonly string QueryKey = "modal";
        private readonly string ProjectName = typeof(ShiftModalService).Assembly.GetName().Name!.Replace('-', '_');

        public ShiftModalService(IJSRuntime jsRuntime, NavigationManager navManager, IDialogService dialogService)
        {
            JsRuntime = jsRuntime;
            NavManager = navManager;
            DialogService = dialogService;
        }

        public async Task<DialogResult?> Open<TComponent>(object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null) where TComponent : ComponentBase
        {
            var ComponentType = typeof(TComponent);

            var fullName = ComponentType!.FullName!.Substring(ProjectName.Length + 1);

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
                UpdateModalQueryUrl(fullName, key, parameters);

                return await OpenDialog(ComponentType, key, parameters);
            }

            return null;
        }

        public string GenerateQueryString(Dictionary<string, string>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";

            return QueryHelpers.AddQueryString("", parameters);
        }

        public void Close(MudDialogInstance mudDialog)
        {
            RemoveFrontModalFromUrl();
            mudDialog.Cancel();
        }

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
                var type = Type.GetType($"{ProjectName}.{modal.Name}");
                if (type != null)
                {
                    _ = OpenDialog(type, modal.Key, modal.Parameters);
                }
            }
        }

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

        private async Task<DialogResult> OpenDialog(Type TComponent, object? itemId = null, Dictionary<string, string>? parameters = null)
        {
            var dParams = new DialogParameters();
            if (itemId != null)
            {
                dParams.Add("Key", itemId);
            }

            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    dParams.Add(item.Key, item.Value);
                }
            }

            var options = new DialogOptions { NoHeader = true };
            var diaRef = DialogService.Show(TComponent, "", dParams, options);
            return await diaRef.Result;
        }

        private List<ModalInfo> ParseModalUrl(string url)
        {
            var uri = NavManager.ToAbsoluteUri(url);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(QueryKey, out var valueFromQueryString))
            {
                var decodedString = System.Net.WebUtility.UrlDecode(valueFromQueryString);
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
            string queryString = "";

            if (modals.Count > 0)
            {
                var modalString = JsonSerializer.Serialize(modals);
                var param = new Dictionary<string, object?>()
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
