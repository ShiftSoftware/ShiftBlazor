using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ShiftValidator : ComponentBase
{
    private ValidationMessageStore? MessageStore;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Parameter]
    public bool EnableFluentValidation { get; set; }

    [Parameter]
    public bool DisableDataAnnotation { get; set; }

    [Parameter]
    public IValidator? Validator { get; set; }

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ShiftValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. " +
                $"For example, you can use {nameof(ShiftValidator)} " +
                $"inside an {nameof(EditForm)}.");
        }

        MessageStore = new(CurrentEditContext);

        CurrentEditContext.OnValidationRequested += ValidationRequestHandler;
        CurrentEditContext.OnFieldChanged += FieldChangedHandler;
    }

    private void FieldChangedHandler(object? sender, FieldChangedEventArgs e)
    {
        MessageStore?.Clear(e.FieldIdentifier);
        CurrentEditContext?.NotifyValidationStateChanged();
    }

    private void ValidationRequestHandler(object? sender, ValidationRequestedEventArgs e)
    {
        if (CurrentEditContext == null)
        {
            return;
        }

        var isValid = true;

        if (!DisableDataAnnotation)
        {
            isValid = CurrentEditContext.ValidateDataAnnotation(null, MessageStore);
        }

        if (EnableFluentValidation && isValid)
        {
            CurrentEditContext.ValidateFluentValidation(null, Validator, MessageStore);
        }
    }
}
