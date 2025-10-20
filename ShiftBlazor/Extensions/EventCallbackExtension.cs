using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Extensions;

internal static class EventCallbackExtension
{
    internal static async Task<bool> CustomInvokeAsync(this EventCallback<ShiftEvent> eventCallback)
    {
        var _event = new ShiftEvent();
        await eventCallback.InvokeAsync(_event);
        return _event.ShouldPreventDefault;
    }

    internal static async Task<bool> PreventableInvokeAsync<T>(this EventCallback<ShiftEvent<T>> eventCallback, T data)
    {
        var _event = new ShiftEvent<T>(data);
        await eventCallback.InvokeAsync(_event);
        return _event.ShouldPreventDefault;
    }
}
