using System.Reflection;
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
    public string? Prefix { get; set; }
    public bool IsCollection { get; set; }
    internal bool IsDefault { get; set; }
    public FilterUIOptions UIOptions { get; set; } = new();

    public abstract ODataFilterGenerator ToODataFilter();

    public static FilterModelBase CreateFilter(string path, Type propertyType, Type? objectType = null, bool isDefault = false)
    {
        var fieldType = FieldType.Identify(propertyType);
        FilterModelBase? filter = null;

        if (fieldType.IsString || fieldType.IsGuid)
        {
            filter = new StringFilterModel { };
            if (objectType != null)
            {
                ((StringFilterModel)filter).DtoType = objectType;
            }
        }
        else if (fieldType.IsEnum)
        {
            filter = new EnumFilterModel
            {
                EnumType = propertyType,
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
        else if (Misc.IsDateTime(propertyType))
        {
            filter = new DateFilterModel { };
        }

        filter ??= new StringFilterModel { };
        filter.Field = path;
        filter.Id = Guid.NewGuid();
        filter.IsDefault = isDefault;

        return filter;
    }
}
