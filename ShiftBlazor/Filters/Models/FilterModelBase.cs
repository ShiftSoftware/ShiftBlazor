using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Collections;
using System.Text.Json;

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
    public FilterUIOptions UIOptions { get; set; } = new();
    internal bool IsDefault { get; set; }
    internal FilterModelBase OriginalState;
    public bool IsNoValueOperator => Operator == ODataOperator.IsEmpty || Operator == ODataOperator.IsNotEmpty;

    public abstract ODataFilterGenerator ToODataFilter();
    public abstract object? ParseValue(object? obj);
    public abstract string? ValueToString();
    public abstract bool HasValue();

    public FilterModelBase()
    {
        OriginalState = (FilterModelBase)this.MemberwiseClone();
    }

    internal FilterModelBase Clone()
    {
        OriginalState = (FilterModelBase)this.MemberwiseClone();
        return OriginalState;
    }

    protected static string? Join(IEnumerable? values)
    {
        if (values == null)
            return null;

        var items = values.Cast<object>().Select(x => x?.ToString());
        return JsonSerializer.Serialize(items);
    }

    protected static IEnumerable<string>? Split(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Reset(bool resetHidden = false)
    {
        if (!IsHidden || resetHidden)
        {
            Value = null;
            Operator = OriginalState.Operator;
        }
    }

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
