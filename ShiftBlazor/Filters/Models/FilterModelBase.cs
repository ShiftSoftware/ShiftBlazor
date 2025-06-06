﻿using System.Reflection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public abstract class FilterModelBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Field { get; set; } = string.Empty;
    public ODataOperator Operator { get; set; }
    public object? Value { get; set; }
    public bool IsHidden { get; set; }
    public bool IsImmediate { get; set; }
    internal bool IsDefault { get; set; }
    public FilterUIOptions UIOptions { get; set; } = new();

    public abstract ODataFilterGenerator ToODataFilter();

    public static FilterModelBase CreateFilter(PropertyInfo field, Type? type = null, bool isDefault = false)
    {
        var fieldType = FieldType.Identify(field.PropertyType);
        FilterModelBase? filter = null;

        if (fieldType.IsString || fieldType.IsGuid)
        {
            filter = new StringFilterModel { };
            if (type != null)
            {
                ((StringFilterModel)filter).DtoType = type;
            }
        }
        else if (fieldType.IsEnum)
        {
            filter = new EnumFilterModel
            {
                EnumType = field.PropertyType,
            };
        }
        else if (fieldType.IsBoolean)
        {
            filter = new BooleanFilterModel { };
        }
        else if (fieldType.IsNumber)
        {
            filter = new NumericFilterModel { };
        }
        else if (Misc.IsDateTime(field.PropertyType))
        {
            filter = new DateFilterModel { };
        }

        filter ??= new StringFilterModel { };
        filter.Field = field.Name;
        filter.Id = Guid.NewGuid();
        filter.IsDefault = isDefault;

        return filter;
    }
}
