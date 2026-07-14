using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class StringFilterModel : FilterModelBase
{
    public Type? DtoType { get; set; }
    public AutocompleteOptions? AutocompleteOptions { get; set; }

    private bool IsMultiValueOperator => Operator is ODataOperator.In or ODataOperator.NotIn;

    public override bool HasValue()
    {
        return (Value is string text && !string.IsNullOrWhiteSpace(text))
            || (Value is IEnumerable<object> list && list.Any())
            || (Value != null && Value is not string && Value is not IEnumerable<object>);
    }

    public override object? ParseValue(object? obj)
    {
        var str = obj?.ToString();

        // In/NotIn operators hold a collection of strings (e.g. from an autocomplete)
        if (IsMultiValueOperator)
            return Split(str)?.ToList();

        return str;
    }

    public override string? ValueToString()
    {
        if (Value is IEnumerable<object> list)
            return Join(list);

        return Value?.ToString();
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);
        var hasValue = this.HasValue();
        var builder = new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Prefix = Prefix,
            IsCollection = IsCollection,
        };

        if (IsNoValueOperator)
        {
            filter.Add(builder);
        }
        else if (this.HasValue())
        {
            builder.Value = Value;
            filter.Add(builder);
        }

        return filter;
    }
}
