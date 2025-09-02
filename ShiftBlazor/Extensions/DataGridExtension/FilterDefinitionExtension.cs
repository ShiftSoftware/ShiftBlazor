using MudBlazor;
using ShiftSoftware.ShiftBlazor.Utils;

namespace System.Collections.Generic;

public static class FilterDefinitionExtension
{
    public static IEnumerable<IEnumerable<string>> ToODataFilter<T>(this ICollection<IFilterDefinition<T>> filterDefinitions)
    {
        return filterDefinitions
            .Where(IsValidFilter)
            .GroupBy(x =>
                new { x.Column?.PropertyName, x.Operator },
                (key, filters) => filters.Select(GetFilterString).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
            .Distinct();
    }

    private static bool IsValidFilter<T>(IFilterDefinition<T> filterDefinition)
    {
        return IsValidFilter(filterDefinition.Value, filterDefinition.Operator);
    }

    private static bool IsValidFilter(object? value, string? filterOperator)
    {
        return !(value == null && filterOperator != FilterOperator.String.Empty && filterOperator != FilterOperator.String.NotEmpty);
    }

    private static string GetFilterString<T>(IFilterDefinition<T> definition)
    {
        var field = definition.Column?.Title ?? definition.Title;
        if (definition.Column != null && !Guid.TryParse(definition.Column.PropertyName, out _))
        {
            field = definition.Column.PropertyName;
        }

        var fieldType = definition.FieldType.InnerType == null && definition.Value != null ? FieldType.Identify(definition.Value.GetType()) : definition.FieldType;

        if (field == null)
            return string.Empty;
        return GetFilterString(field, definition.Operator!, definition.Value, fieldType);
    }

    private static string GetFilterString(string field, string op, object? value, FieldType? fieldType = null)
    {
        var _field = Misc.GetFieldFromPropertyPath(field);
        var _value = ODataFilterGenerator.GetValueString(value, fieldType!);
        var filterTemplate = ODataFilterGenerator.CreateFilterTemplate(op);

        return string.Format(filterTemplate, _field, _value);
    }
}
