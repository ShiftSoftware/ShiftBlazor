using FluentValidation;
using FluentValidation.Internal;
using ShiftSoftware.ShiftBlazor.Extensions.EditContext;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

public static class EditContextExtension
{
    private static readonly char[] Separators = { '.', '[' };

    private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();

    public static bool Validate(this EditContext editContext, in List<FieldIdentifier> fields, IValidator? validator = null, ValidationMessageStore? messageStore = null)
    {
        var isValid = true;

        messageStore ??= new ValidationMessageStore(editContext);

        isValid = editContext.ValidateDataAnnotation(fields, messageStore);

        // if the DataAnnotation validator returns false,
        // then don't run the FluentValidation validator 
        if (isValid)
        {
            isValid = editContext.ValidateFluentValidation(fields, validator, messageStore);
        }

        return isValid;
    }

    public static bool ValidateDataAnnotation(this EditContext editContext, in List<FieldIdentifier>? fields, ValidationMessageStore? messageStore = null)
    {
        messageStore ??= new ValidationMessageStore(editContext);
        var isValid = true;
        var results = new List<ValidationResult>();

        editContext.ClearErrors(fields, messageStore);

        if (fields == null)
        {
            var context = new ValidationContext(editContext.Model);
            isValid = Validator.TryValidateObject(editContext.Model, context, results, true);
        }
        else
        {
            foreach (var field in fields)
            {
                if (TryGetValidatableProperty(field, out var propertyInfo))
                {
                    var propertyValue = propertyInfo?.GetValue(editContext.Model);

                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property '{field.FieldName}' not found on model type '{editContext.Model.GetType().FullName}'.");
                    }

                    var validationContext = new ValidationContext(editContext.Model)
                    {
                        MemberName = propertyInfo.Name
                    };

                    if (!Validator.TryValidateProperty(propertyValue, validationContext, results))
                    {
                        isValid = false;
                    }
                }

            }
        }

        foreach (var error in results)
        {
            if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
            {
                foreach (var name in error.MemberNames)
                {
                    var field = editContext.ToFieldIdentifier(name);
                    messageStore.Add(field, error.ErrorMessage);
                }
            }
        }

        editContext.NotifyValidationStateChanged();
        return isValid;
    }

    public static bool ValidateFluentValidation(this EditContext editContext, in List<FieldIdentifier>? fields, IValidator? validator = null, ValidationMessageStore? messageStore = null)
    {
        messageStore ??= new ValidationMessageStore(editContext);
        var isValid = true;

        editContext.ClearErrors(fields, messageStore);

        IntersectingCompositeValidatorSelector? compositeSelector = null;
        if (fields != null)
        {
            var propertyPaths = fields.Select(editContext.ToFluentPropertyPath).Where(x => !string.IsNullOrWhiteSpace(x));

            if (!propertyPaths.Any())
            {
                return isValid;
            }

            var context = new ValidationContext<object>(editContext.Model);
            var fluentValidationValidatorSelector = context.Selector;
            var changedPropertySelector = ValidationContext<object>.CreateWithOptions(editContext.Model, strategy =>
            {
                strategy.IncludeProperties(propertyPaths.ToArray());
            }).Selector;

            compositeSelector = new([fluentValidationValidatorSelector, changedPropertySelector]);
        }

        validator ??= ScanValidator(editContext.Model.GetType());

        if (validator != null)
        {
            var context = compositeSelector == null 
                ? new ValidationContext<object>(editContext.Model)
                : new ValidationContext<object>(editContext.Model, new PropertyChain(), compositeSelector);
            
            var result = validator.Validate(context);

            foreach (var error in result.Errors)
            {
                var field = editContext.ToFieldIdentifier(error.PropertyName);
                messageStore.Add(field, error.ErrorMessage);
            }

            isValid = result.IsValid;
        }

        editContext.NotifyValidationStateChanged();
        return isValid;
    }

    public static void ClearErrors(this EditContext editContext, ValidationMessageStore? messageStore = null)
    {
        editContext.ClearErrors(null, messageStore);
    }

    public static void ClearErrors(this EditContext editContext, in List<FieldIdentifier>? fields, ValidationMessageStore? messageStore = null)
    {
        messageStore ??= new ValidationMessageStore(editContext);
        if (fields == null)
        {
            messageStore.Clear();
        }
        else
        {
            foreach (var field in fields)
            {
                messageStore.Clear(field);
            }
        }
        editContext?.NotifyValidationStateChanged();
    }

    public static void DisplayErrors(this EditContext context, Dictionary<string, List<string>> errors, ValidationMessageStore? messageStore = null)
    {
        messageStore ??= new ValidationMessageStore(context);

        foreach (var err in errors)
        {
            var field = ToFieldIdentifier(context, err.Key);
            messageStore.Add(field, err.Value);
        }

        context.NotifyValidationStateChanged();
    }

    public static FieldIdentifier ToFieldIdentifier(this EditContext editContext, in string propertyPath)
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

    public static string ToFluentPropertyPath(this EditContext editContext, FieldIdentifier fieldIdentifier)
    {
        var nodes = new Stack<PropertyPathNode>();
        nodes.Push(new PropertyPathNode()
        {
            ModelObject = editContext.Model,
        });

        while (nodes.Any())
        {
            var currentNode = nodes.Pop();
            var currentModelObject = currentNode.ModelObject;

            if (currentModelObject == fieldIdentifier.Model)
            {
                return BuildPropertyPath(currentNode, fieldIdentifier);
            }

            var nonPrimitiveProperties = currentModelObject?.GetType()
                .GetProperties()
                .Where(prop => !prop.PropertyType.IsPrimitive || prop.PropertyType.IsArray) ?? new List<PropertyInfo>();

            foreach (var nonPrimitiveProperty in nonPrimitiveProperties)
            {
                var instance = nonPrimitiveProperty.GetValue(currentModelObject);

                if (instance == fieldIdentifier.Model)
                {
                    var node = new PropertyPathNode()
                    {
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name,
                        ModelObject = instance
                    };

                    return BuildPropertyPath(node, fieldIdentifier);
                }

                if (instance is IEnumerable enumerable)
                {
                    var itemIndex = 0;
                    foreach (var item in enumerable)
                    {
                        nodes.Push(new PropertyPathNode()
                        {
                            ModelObject = item,
                            Parent = currentNode,
                            PropertyName = nonPrimitiveProperty.Name,
                            Index = itemIndex++
                        });
                    }
                }
                else if (instance is not null)
                {
                    nodes.Push(new PropertyPathNode()
                    {
                        ModelObject = instance,
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name
                    });
                }
            }
        }

        return string.Empty;
    }

    public static IValidator? ScanValidator(Type modelType)
    {
        var assemblyScanner = AssemblyScanner
            .FindValidatorsInAssembly(modelType?.Assembly)
            .FirstOrDefault(x => x.InterfaceType.GenericTypeArguments.First() == modelType);

        if (assemblyScanner != null)
        {
            return Activator.CreateInstance(assemblyScanner.ValidatorType) as IValidator;
        }

        return null;
    }

    private static bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
    {
        var cacheKey = (ModelType: fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
        if (!_propertyInfoCache.TryGetValue(cacheKey, out propertyInfo))
        {
            // DataAnnotations only validates public properties, so that's all we'll look for
            // If we can't find it, cache 'null' so we don't have to try again next time
            propertyInfo = cacheKey.ModelType.GetProperty(cacheKey.FieldName);

            // No need to lock, because it doesn't matter if we write the same value twice
            _propertyInfoCache[cacheKey] = propertyInfo;
        }

        return propertyInfo != null;
    }

    private static string BuildPropertyPath(PropertyPathNode currentNode, FieldIdentifier fieldIdentifier)
    {
        var pathParts = new List<string>();
        pathParts.Add(fieldIdentifier.FieldName);
        var next = currentNode;

        while (next is not null)
        {
            if (!string.IsNullOrEmpty(next.PropertyName))
            {
                if (next.Index is not null)
                {
                    pathParts.Add($"{next.PropertyName}[{next.Index}]");
                }
                else
                {
                    pathParts.Add(next.PropertyName);
                }
            }

            next = next.Parent;
        }

        pathParts.Reverse();

        return string.Join('.', pathParts);
    }

}
