using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class NumericFilterModel : FilterModelBase
{
    public object Value2 { get; set; } = 0d;
    public double PercentValue { get; set; }
    public NumericFilterOperator? SelectedNumOperator { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var op = ODataOperator.Equal;
        object value = 0;
        var castType = string.Empty;
        var stringFilter = string.Empty;
        var filter = new ODataFilterGenerator(true, Id);

        var negate = Operator == ODataOperator.NotEqual;

        if (Operator == ODataOperator.Equal || negate)
        {
            switch (SelectedNumOperator)
            {
                case NumericFilterOperator.Positive:
                    op = negate ? ODataOperator.LessThan : ODataOperator.GreaterThan;
                    break;

                case NumericFilterOperator.Negative:
                    op = negate ? ODataOperator.GreaterThan : ODataOperator.LessThan;
                    break;

                case NumericFilterOperator.Even:
                    stringFilter = $"{Field} mod 2 eq 0";
                    break;

                case NumericFilterOperator.Odd:
                    stringFilter = $"{Field} mod 2 eq 1";
                    break;

                case NumericFilterOperator.WholeNumbers:
                    stringFilter = $"floor({Field}) eq {Field}";
                    break;

                case NumericFilterOperator.Equal:
                    op = negate ? ODataOperator.NotEqual : ODataOperator.Equal;
                    value = Value;
                    break;

                case NumericFilterOperator.GreaterThan:
                    op = negate ? ODataOperator.LessThanOrEqual : ODataOperator.GreaterThan;
                    value = Value;
                    break;

                case NumericFilterOperator.GreaterThanOrEqual:
                    op = negate ? ODataOperator.LessThan : ODataOperator.GreaterThanOrEqual;
                    value = Value;
                    break;

                case NumericFilterOperator.LessThan:
                    op = negate ? ODataOperator.GreaterThanOrEqual : ODataOperator.LessThan;
                    value = Value;
                    break;

                case NumericFilterOperator.LessThanOrEqual:
                    op = negate ? ODataOperator.GreaterThan : ODataOperator.LessThanOrEqual;
                    value = Value;
                    break;

                case NumericFilterOperator.Between:
                    stringFilter = $"{Field} gt {Value} and {Field} lt {Value2}";
                    break;

                case NumericFilterOperator.MultipleOf:
                    stringFilter = $"cast({Field}, Edm.Decimal) mod {Value} eq 0";
                    break;

                case NumericFilterOperator.StartsWith:
                    op = negate ? ODataOperator.NotStartsWith : ODataOperator.StartsWith;
                    value = Value.ToString();
                    castType = ODataPrimitiveTypes.String;
                    break;

                case NumericFilterOperator.EndsWith:
                    op = negate ? ODataOperator.NotEndsWith : ODataOperator.EndsWith;
                    value = Value.ToString();
                    castType = ODataPrimitiveTypes.String;
                    break;

                case NumericFilterOperator.PercentageOf:
                    stringFilter = $"{Field} mul {PercentValue / 100} le {Value}";
                    break;
            }
        }
        else
        {
            op = Operator;
        }

        if (string.IsNullOrWhiteSpace(stringFilter))
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = op,
                Value = value,
                CastToType = castType
            });
        }
        else
        {
            if (negate)
                stringFilter += " eq false";
            filter.Add(stringFilter);
        }

        return filter;
    }
}
