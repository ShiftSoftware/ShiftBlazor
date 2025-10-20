using Microsoft.JSInterop;

namespace ShiftSoftware.ShiftBlazor.Services;

public class ClipboardService
{
    private readonly IJSRuntime JsRuntime;

    public ClipboardService(IJSRuntime jsRuntime)
    {
        JsRuntime = jsRuntime;
    }

    /// <summary>
    ///     Writes the specified text string to the system clipboard.
    /// </summary>
    /// <param name="text">The string to be written to the clipboard.</param>
    public ValueTask WriteTextAsync(string text)
    {
        return JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}
