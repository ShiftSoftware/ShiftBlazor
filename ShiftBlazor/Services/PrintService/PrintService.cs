using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components.Print;
using System.IO;

namespace ShiftSoftware.ShiftBlazor.Services;

public class PrintService
{
    private readonly HttpClient HttpClient;
    private readonly IJSRuntime JsRuntime;
    private readonly IDialogService DialogService;

    public PrintService(HttpClient http, IJSRuntime jSRuntime, IDialogService dialogService)
    {
        HttpClient = http;
        JsRuntime = jSRuntime;
        DialogService = dialogService;
    }

    public async Task PrintAsync(string url, string id, Dictionary<string, string>? queryItems = null)
    {
        var tokenResult = await HttpClient.GetAsync($"{url}/print-token/{id}");
        var token = await tokenResult.Content.ReadAsStringAsync();

        var query = queryItems?.Select(x => $"{x.Key}={x.Value}").ToList() ?? [];
        var queryString = string.Join('&', query);

        var documentPath = $"{url}/print/{id}";
        var documentUrl = $"{documentPath}?{token}&{queryString}";

        //Open /print endpoint with the obtained token
        await JsRuntime.InvokeVoidAsync("open", documentUrl, "_blank");
    }


    public async Task<IDialogReference> OpenPrintFormAsync(string url, string id, PrintFormConfig config, DialogOptions? dialogOptions = null)
    {
        var parameters = new DialogParameters
        {
            { "Options", config },
            { "Url", url },
            { "Key", id }
        };

        dialogOptions ??= new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            NoHeader = true,
            CloseOnEscapeKey = false,
        };

        var dialogReference = await DialogService.ShowAsync<PrintForm>("", parameters, dialogOptions);

        return dialogReference;
    }
}
