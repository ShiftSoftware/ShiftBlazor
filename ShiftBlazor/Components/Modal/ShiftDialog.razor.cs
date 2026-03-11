using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ShiftSoftware.ShiftBlazor.Components.Modal;

public partial class ShiftDialog
{
    private IMudDialogInstance _instance;
    private IDialogReference _reference;

    public ShiftDialog()
    {
        _instance = new DialogInstance(this);
        _reference = new DialogReference(this);
    }

    [Inject] IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter]
    public DialogOptions Options { get; set; } = new();

    //[Parameter]
    //public string? Title { get; set; }

    public string ElementId => $"dialog-{Id}";
    public Task<DialogResult?> Result => _reference.Result;

    internal void OpenInlineDialog()
    {
        JsRuntime.InvokeVoidAsync("openDialog", ElementId);
    }

    internal void CloseInlineDialog()
    {
        JsRuntime.InvokeVoidAsync("closeDialog", ElementId);
    }

    public IDialogReference ShowAsync()
    {
        if (_reference.Result.IsCompleted)
            _reference = new DialogReference(this);
        OpenInlineDialog();
        return _reference;
    }

    public void Close()
    {
        this.Close(DialogResult.Ok<object?>(null));
    }

    public void Close(DialogResult? result)
    {
        this.Dismiss(result);
    }

    public bool Dismiss(DialogResult? result)
    {
        if (_reference.Dismiss(result))
        {
            CloseInlineDialog();
            return true;
        }

        return false;
    }

    private class DialogReference : IDialogReference
    {
        private readonly TaskCompletionSource<DialogResult?> _resultCompletion = new();
        private readonly ShiftDialog _dialog;

        public Guid Id => _dialog.Id;
        public string ElementId => _dialog.ElementId;

        public DialogReference(ShiftDialog dialog)
        {
            _dialog = dialog;
        }

        public RenderFragment? RenderFragment { get; set; }

        public Task<DialogResult?> Result => _resultCompletion.Task;

        public TaskCompletionSource<bool> RenderCompleteTaskCompletionSource { get; } = new();

        public DialogOptions? Options { get; private set; }

        public object? Dialog { get; private set; }

        public void Close()
        {
            this.Close();
        }

        public void Close(DialogResult? result)
        {
            this.Close(result);
        }

        public bool Dismiss(DialogResult? result)
        {
            return _resultCompletion.TrySetResult(result);
        }

        public async Task<T?> GetReturnValueAsync<[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] T>()
        {
            var result = await Result;
            try
            {
                return (T?)result?.Data;
            }
            catch (InvalidCastException)
            {
                Debug.WriteLine($"Could not cast return value to {typeof(T)}, returning default.");
                return default;
            }
        }

        public void InjectDialog(object inst) { }
        public void InjectRenderFragment(RenderFragment rf) { }
    }

    private class DialogInstance : IMudDialogInstance
    {
        private ShiftDialog _dialog;

        public DialogInstance(ShiftDialog dialog)
        {
            _dialog = dialog;
        }

        public Guid Id => _dialog.Id;
        public string ElementId => _dialog.ElementId;

        public DialogOptions Options { get; } = new();

        public string? Title { get; } = string.Empty;

        public void Cancel() => this.Close(DialogResult.Cancel());

        public void CancelAll()
        {
            this.Close();
        }

        public void Close()
        {
            this.Close(DialogResult.Ok<object?>(null));
        }

        public void Close(DialogResult dialogResult)
        {
            _dialog.Close(dialogResult);
        }

        public void Close<T>(T returnValue)
        {
            var dialogResult = DialogResult.Ok<T>(returnValue);
            _dialog.Close(dialogResult);
        }

        public Task SetOptionsAsync(DialogOptions options)
        {
            return Task.CompletedTask;
        }

        public Task SetTitleAsync(string? title)
        {
            return Task.CompletedTask;
        }

        public void StateHasChanged()
        {
            _dialog.StateHasChanged();
        }
    }
}
