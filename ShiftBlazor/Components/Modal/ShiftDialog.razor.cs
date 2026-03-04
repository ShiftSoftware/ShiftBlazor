using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Components.Modal;

public partial class ShiftDialog : IDialogReference
{

    private readonly TaskCompletionSource<DialogResult?> _resultCompletion = new();
    private readonly ShiftDialogService _dialogService;
    private IMudDialogInstance? _instance;

    //private readonly IDialogService _dialogService;

    public ShiftDialog()
    {
        _dialogService = new();
        _dialogService.InjectDialog(this);
    }

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Parameter]
    public Guid Id { get; set; }

    public string ElementId => $"dialog-{Id}";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [CascadingParameter]
    public MudDialogProvider? MudDialogProvider { get; set; }

    private string htmlId => $"dialog-{Id}";

    protected override void OnInitialized()
    {
        //DialogService.DialogInstanceAddedAsync += DialogService_DialogInstanceAddedAsync;
        //DialogService.OnDialogCloseRequested += DialogService_OnDialogCloseRequested;

        _dialogService.DialogInstanceAddedAsync += _dialogService_DialogInstanceAddedAsync;
        _dialogService.OnDialogCloseRequested += _dialogService_OnDialogCloseRequested;

        _instance = new DialogInstance(this, MudDialogProvider);
    }

    private Task _dialogService_DialogInstanceAddedAsync(IDialogReference arg)
    {
        Console.WriteLine("event2: Dialog Added");
        return Task.CompletedTask;
    }

    private void _dialogService_OnDialogCloseRequested(IDialogReference arg1, DialogResult? arg2)
    {
        Console.WriteLine("event2: Dialog Closed");
    }

    internal void OpenInlineDialog()
    {
        JsRuntime.InvokeVoidAsync("openDialog", htmlId);
    }

    internal void CloseInlineDialog()
    {
        JsRuntime.InvokeVoidAsync("closeDialog", htmlId);
    }

    public IDialogReference ShowAsync()
    {
        _dialogService.ShowAsync<DummyComponent>();
        OpenInlineDialog();
        return this;
    }

    //public DialogOptions Options { get; set; } = new();

    //[Parameter]
    //public string? Title { get; set; }

    public RenderFragment? RenderFragment { get; set; }

    public Task<DialogResult?> Result => _resultCompletion.Task;
        
    public TaskCompletionSource<bool> RenderCompleteTaskCompletionSource { get; } = new();

    public DialogOptions? Options { get; private set; }

    public object? Dialog { get; private set; }

    public void Close()
    {
        _dialogService.Close(this);
    }

    public void Close(DialogResult? result)
    {
        _dialogService.Close(this, result);
    }

    public bool Dismiss(DialogResult? result)
    {
        if (_resultCompletion.TrySetResult(result))
        {
            CloseInlineDialog();
            return true;
        }

        return false;
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

    public void InjectDialog(object inst)
    {
        Dialog = inst;
    }

    public void InjectRenderFragment(RenderFragment rf)
    {
        //throw new NotImplementedException();
    }


    internal class DummyComponent : ComponentBase;

    private class DialogInstance : IMudDialogInstance
    {
        private ShiftDialog _dialog;
        private MudDialogProvider _dialogProvider;

        public DialogInstance(ShiftDialog dialog, MudDialogProvider dialogProvider)
        {
            _dialog = dialog;
            _dialogProvider = dialogProvider;
        }

        public Guid Id => _dialog.Id;
        public string ElementId => _dialog.ElementId;

        public DialogOptions Options { get; } = new();

        public string? Title => string.Empty;

        public void Cancel() => this.Close(DialogResult.Cancel());

        public void CancelAll()
        {
            _dialogProvider?.DismissAll();
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
            //throw new NotImplementedException();
        }

        public Task SetTitleAsync(string? title)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        public void StateHasChanged()
        {
            _dialog.StateHasChanged();
        }
    }
}
