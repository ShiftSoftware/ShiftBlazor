using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

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
        MessageStore?.Clear();

        var model = CurrentEditContext!.Model;

        if (!DisableDataAnnotation)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, context, results, true);

            foreach (var error in results)
            {
                if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
                {
                    foreach (var name in error.MemberNames)
                    {
                        var field = ToFieldIdentifier(CurrentEditContext, name);
                        MessageStore?.Add(field, error.ErrorMessage);
                    }
                }
            }
        }

        if (EnableFluentValidation)
        {
            if (Validator == null)
            {
                var type = model.GetType();
                var assemblyScanner = AssemblyScanner
                    .FindValidatorsInAssembly(type?.Assembly)
                    .FirstOrDefault(x => x.InterfaceType.GenericTypeArguments.First() == type);
                if (assemblyScanner != null)
                {
                    Validator = Activator.CreateInstance(assemblyScanner.ValidatorType) as IValidator;
                }
            }

            if (Validator != null)
            {
                var context = new ValidationContext<object>(model);
                var result = Validator.Validate(context);

                foreach (var error in result.Errors)
                {
                    var field = ToFieldIdentifier(CurrentEditContext, error.PropertyName);
                    MessageStore?.Add(field, error.ErrorMessage);
                }
            }
        }

        CurrentEditContext.NotifyValidationStateChanged();
    }

    public void ClearErrors()
    {
        MessageStore?.Clear();
        CurrentEditContext?.NotifyValidationStateChanged();
    }

    public void DisplayErrors(Dictionary<string, List<string>> errors)
    {
        if (CurrentEditContext is not null)
        {
            ShiftValidator.DisplayErrors(CurrentEditContext, MessageStore, errors);
        }
    }

    public static void DisplayErrors(EditContext context, ValidationMessageStore? messageStore, Dictionary<string, List<string>> errors)
    {
        messageStore ??= new ValidationMessageStore(context);

        foreach (var err in errors)
        {
            var field = ToFieldIdentifier(context, err.Key);
            messageStore?.Add(field, err.Value);
        }

        context.NotifyValidationStateChanged();
    }

    private static readonly char[] Separators = { '.', '[' };

    private static FieldIdentifier ToFieldIdentifier(in EditContext editContext, in string propertyPath)
    {
        // https://github.com/Blazored/FluentValidation/blob/8afbf82cd16ea3ef08cbd5f73244761ecebf50d5/src/Blazored.FluentValidation/EditContextFluentValidationExtensions.cs#L181
        // This code is taken from an article by Steve Sanderson (https://blog.stevensanderson.com/2019/09/04/blazor-fluentvalidation/)
        // all credit goes to him for this code.

        // This method parses property paths like 'SomeProp.MyCollection[123].ChildProp'
        // and returns a FieldIdentifier which is an (instance, propName) pair. For example,
        // it would return the pair (SomeProp.MyCollection[123], "ChildProp"). It traverses
        // as far into the propertyPath as it can go until it finds any null instance.

        var obj = editContext.Model;
        var nextTokenEnd = propertyPath.IndexOfAny(Separators);

        // Optimize for a scenario when parsing isn't needed.
        if (nextTokenEnd < 0)
        {
            return new FieldIdentifier(obj, propertyPath);
        }

        ReadOnlySpan<char> propertyPathAsSpan = propertyPath;

        while (true)
        {
            var nextToken = propertyPathAsSpan.Slice(0, nextTokenEnd);
            propertyPathAsSpan = propertyPathAsSpan.Slice(nextTokenEnd + 1);

            object? newObj;
            if (nextToken.EndsWith("]"))
            {
                // It's an indexer
                // This code assumes C# conventions (one indexer named Item with one param)
                nextToken = nextToken.Slice(0, nextToken.Length - 1);
                var prop = obj.GetType().GetProperty("Item");

                if (prop is not null)
                {
                    // we've got an Item property
                    var indexerType = prop.GetIndexParameters()[0].ParameterType;
                    var indexerValue = Convert.ChangeType(nextToken.ToString(), indexerType);

                    newObj = prop.GetValue(obj, new[] { indexerValue });
                }
                else
                {
                    // If there is no Item property
                    // Try to cast the object to array
                    if (obj is object[] array)
                    {
                        var indexerValue = int.Parse(nextToken);
                        newObj = array[indexerValue];
                    }
                    else if (obj is IReadOnlyList<object> readOnlyList)
                    {
                        // Addresses an issue with collection expressions in C# 12 regarding IReadOnlyList:
                        // Generates a <>z__ReadOnlyArray which:
                        // - lacks an Item property, and
                        // - cannot be cast to object[] successfully.
                        // This workaround accesses elements directly using an indexer.
                        var indexerValue = int.Parse(nextToken);
                        newObj = readOnlyList[indexerValue];
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not find indexer on object of type {obj.GetType().FullName}.");
                    }
                }
            }
            else
            {
                // It's a regular property
                var prop = obj.GetType().GetProperty(nextToken.ToString());
                if (prop == null)
                {
                    throw new InvalidOperationException($"Could not find property named {nextToken.ToString()} on object of type {obj.GetType().FullName}.");
                }
                newObj = prop.GetValue(obj);
            }

            if (newObj == null)
            {
                // This is as far as we can go
                return new FieldIdentifier(obj, nextToken.ToString());
            }

            obj = newObj;

            nextTokenEnd = propertyPathAsSpan.IndexOfAny(Separators);
            if (nextTokenEnd < 0)
            {
                return new FieldIdentifier(obj, propertyPathAsSpan.ToString());
            }
        }
    }

}
