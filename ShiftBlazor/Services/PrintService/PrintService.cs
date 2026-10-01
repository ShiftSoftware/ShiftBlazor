using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Components.Print;
using ShiftSoftware.ShiftEntity.Model;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Services;

public class PrintService
{
    private readonly HttpClient HttpClient;
    private readonly IJSRuntime JsRuntime;
    private readonly IDialogService DialogService;
    private readonly MessageService? MessageService;

    public PrintService(HttpClient http, IJSRuntime jSRuntime, IDialogService dialogService)
    {
        HttpClient = http;
        JsRuntime = jSRuntime;
        DialogService = dialogService;
    }

    public PrintService(HttpClient http, IJSRuntime jSRuntime, IDialogService dialogService, MessageService messageService)
        : this(http, jSRuntime, dialogService)
    {
        MessageService = messageService;
    }

    public async Task PrintAsync(string url, string id, Dictionary<string, string>? queryItems = null)
    {
        await TryPrintAsync(url, id, queryItems);
    }

    /// <summary>Returns an error for a print form to display, or null after opening the document.</summary>
    public async Task<string?> TryPrintAsync(string url, string id, Dictionary<string, string>? queryItems = null)
    {
        string token;
        try
        {
            using var tokenResult = await HttpClient.GetAsync($"{url}/print-token/{id}");
            if (!tokenResult.IsSuccessStatusCode)
            {
                var message = await ReadErrorAsync(tokenResult);
                await ReportErrorAsync(message);
                return message;
            }
            token = await tokenResult.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            const string message = "Couldn't prepare the print. Check your connection and try again.";
            await ReportErrorAsync(message);
            return message;
        }

        var query = queryItems?.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}").ToList() ?? [];
        var queryString = string.Join('&', query);

        var documentPath = $"{url}/print/{id}";
        var documentUrl = $"{documentPath}?{token}&{queryString}";

        //Open /print endpoint with the obtained token
        await JsRuntime.InvokeVoidAsync("open", documentUrl, "_blank");
        return null;
    }

    private async Task ReportErrorAsync(string message)
    {
        if (MessageService is not null)
        {
            MessageService.Error(message);
            return;
        }

        // Callers using the original constructor can still see preparation errors.
        var parameters = new DialogParameters
        {
            { "Message", new Message("Print error", message) },
            { "Color", Color.Error },
            { "Icon", Icons.Material.Outlined.Error },
        };
        await DialogService.ShowAsync<PopupMessage>("", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, NoHeader = true });
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = $"Couldn't prepare the print (HTTP {(int)response.StatusCode}).";
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<ShiftEntityResponse>(body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return !string.IsNullOrWhiteSpace(error?.Message?.Body) ? error.Message.Body
                : !string.IsNullOrWhiteSpace(error?.Message?.Title) ? error.Message.Title
                : fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
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

        return await DialogService.ShowAsync<PrintForm>("", parameters, dialogOptions);
    }
}
