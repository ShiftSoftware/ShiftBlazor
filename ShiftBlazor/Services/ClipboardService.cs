using Microsoft.JSInterop;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ClipboardService
    {
        private readonly IJSRuntime JsRuntime;

        public ClipboardService(IJSRuntime jsRuntime)
        {
            JsRuntime = jsRuntime;
        }

        public ValueTask WriteTextAsync(string text)
        {
            return JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
