using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class NumericFilterModel : FilterModelBase
{
    public object Value2 { get; set; }
    public double PercentValue { get; set; }
    public NumericFilterOperator? SelectedNumOperator { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var op = ODataOperator.Equal;
        object value = 0;
        var castType = string.Empty;
        var stringFilter = string.Empty;
        var filter = new ODataFilterGenerator(true, Id);

        if (Operator == ODataOperator.Equal)
        {
            if (SelectedNumOperator == NumericFilterOperator.Positive)
            {
                op = ODataOperator.GreaterThan;
            }
            else if (SelectedNumOperator == NumericFilterOperator.Negative)
            {
                op = ODataOperator.LessThan;
            }
            else if (SelectedNumOperator == NumericFilterOperator.Even)
            {
                stringFilter = $"{Field} mod 2 eq 0";
            }
            else if (SelectedNumOperator == NumericFilterOperator.Odd)
            {
                stringFilter = $"{Field} mod 2 eq 1";
            }
            else if (SelectedNumOperator == NumericFilterOperator.WholeNumbers)
            {
                stringFilter = $"floor({Field}) eq {Field}";
            }
            else if (SelectedNumOperator == NumericFilterOperator.Equal)
            {
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.NotEqual)
            {
                op = ODataOperator.NotEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.GreaterThan)
            {
                op = ODataOperator.GreaterThan;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.GreaterThanOrEqual)
            {
                op = ODataOperator.GreaterThanOrEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.LessThan)
            {
                op = ODataOperator.LessThan;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.LessThanOrEqual)
            {
                op = ODataOperator.LessThanOrEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOperator.Between)
            {
                stringFilter = $"{Field} gt {Value} and {Field} lt {Value2}";
            }
            else if (SelectedNumOperator == NumericFilterOperator.MultipleOf)
            {
                stringFilter = $"cast({Field}, Edm.Decimal) mod {Value} eq 0";
            }
            else if (SelectedNumOperator == NumericFilterOperator.StartsWith)
            {
                op = ODataOperator.StartsWith;
                value = Value.ToString();
                castType = ODataPrimitiveTypes.String;
            }
            else if (SelectedNumOperator == NumericFilterOperator.EndsWith)
            {
                op = ODataOperator.EndsWith;
                value = Value.ToString();
                castType = ODataPrimitiveTypes.String;
            }
            else if (SelectedNumOperator == NumericFilterOperator.PercentageOf)
            {
                stringFilter = $"{Field} mul {PercentValue / 100} le {Value}";
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
            filter.Add(stringFilter);
        }

        return filter;
    }
}
