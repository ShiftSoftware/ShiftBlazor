using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class StringFilterModel : FilterModelBase
{
    public Type? DtoType { get; set; }
    public AutocompleteOptions? AutocompleteOptions { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);
        var hasValue = !(Value == null || Value is string value && string.IsNullOrWhiteSpace(value));

        if (Value is IEnumerable<object> list && !list.Any())
        {
            return filter;
        }

        if (hasValue || hasValue && (Operator == Enums.ODataOperator.IsEmpty || Operator == Enums.ODataOperator.IsNotEmpty))
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator,
                Value = Value,
                Prefix = Prefix,
                IsCollection = IsCollection,
            });
        }

        return filter;
    }
}
