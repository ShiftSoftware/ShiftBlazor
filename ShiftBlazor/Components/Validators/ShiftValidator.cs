using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Reflection.Metadata;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ShiftValidator : ComponentBase, IDisposable
{
    // TODO
    // - Add ValidationOptions support
    // Validate list of complex objects

    internal ValidationMessageStore? MessageStore;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Parameter]
    public bool EnableFluentValidation { get; set; }

    [Parameter]
    public bool DisableDataAnnotation { get; set; }

    [Parameter]
    public bool DisableValidationOnFieldChange { get; set; }

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
        if (DisableValidationOnFieldChange)
        {
            MessageStore?.Clear(e.FieldIdentifier);
            CurrentEditContext!.NotifyValidationStateChanged();
        }
        else
        {
            Validate(e.FieldIdentifier);
        }
    }

    private void ValidationRequestHandler(object? sender, ValidationRequestedEventArgs e)
    {
        Validate();
    }

    private void Validate(in FieldIdentifier? field = null)
    {
        ArgumentNullException.ThrowIfNull(CurrentEditContext);

        var fields = field.HasValue ? new List<FieldIdentifier> { field.Value } : null;

        var isValid = true;

        if (!DisableDataAnnotation)
        {
            isValid = CurrentEditContext.ValidateDataAnnotation(fields, MessageStore);
        }

        if (EnableFluentValidation && isValid)
        {
            CurrentEditContext.ValidateFluentValidation(fields, Validator, MessageStore);
        }

        CurrentEditContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (CurrentEditContext is not null)
        {
            CurrentEditContext.OnValidationRequested -= ValidationRequestHandler;
            CurrentEditContext.OnFieldChanged -= FieldChangedHandler;
        }

        GC.SuppressFinalize(this);
    }
}
