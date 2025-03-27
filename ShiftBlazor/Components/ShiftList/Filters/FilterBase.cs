using System.Globalization;
using System.Reflection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using static ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters.DateFilterInput;
using static ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters.NumericFilterInput;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public abstract class FilterBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Field { get; set; }
    public ODataOperator Operator { get; set; }
    public object? Value { get; set; }
    public bool IsHidden { get; set; }
    public bool IsReadOnly { get; set; }

    public abstract ODataFilterGenerator ToODataFilter();

    public static FilterBase CreateFilter(PropertyInfo field, Type? type = null)
    {
        var fieldType = FieldType.Identify(field.PropertyType);
        FilterBase? filter = null;

        if (fieldType.IsString || fieldType.IsGuid)
        {
            filter = new _StringFilter { };
            if (type != null) 
            {
                ((_StringFilter)filter).DtoType = type;
            }
        }
        else if (fieldType.IsEnum)
        {
            filter = new _EnumFilter
            {
                EnumType = field.PropertyType,
            };
        }
        else if (fieldType.IsBoolean)
        {
            filter = new _BooleanFilter { };
        }
        else if (fieldType.IsNumber)
        {
            filter = new _NumericFilter { };
        }
        else if (Misc.IsDateTime(field.PropertyType))
        {
            filter = new _DateFilter { };
        }

        filter ??= new _StringFilter { };
        filter.Field = field.Name;
        filter.Id = Guid.NewGuid();

        return filter;
    }
}

public class _BooleanFilter : FilterBase
{
    public override ODataFilterGenerator ToODataFilter()
    {
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = ODataOperator.Equal,
            Value = Value is bool val && val == true,
        });
    }
}

public class _EnumFilter : FilterBase
{
    public Type? EnumType { get; set; }
    public Type? EnumTypeToUse
    {
        get
        {
            if (EnumType != null)
            {
                return EnumType.IsEnum ? EnumType : Nullable.GetUnderlyingType(EnumType);
            }
            return null;
        }
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = ODataOperator.Equal,
            Value = Value
        });
    }
}

public class _StringFilter : FilterBase
{
	public Type? DtoType { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Value = Value
        });
    }
}

public class _NumericFilter : FilterBase
{
    public new double Value { get; set; }
    public double Value2 { get; set; }
    public double PercentValue { get; set; }
    public NumericFilterOption? SelectedNumOperator { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var op = ODataOperator.Equal;
        object value = 0;
        var castType = string.Empty;
        var stringFilter = string.Empty;
        var filter = new ODataFilterGenerator(true, Id);

        if (Operator == ODataOperator.Equal)
        {
            if (SelectedNumOperator == NumericFilterOption.Positive)
            {
                op = ODataOperator.GreaterThan;
            }
            else if (SelectedNumOperator == NumericFilterOption.Negative)
            {
                op = ODataOperator.LessThan;
            }
            else if (SelectedNumOperator == NumericFilterOption.Even)
            {
                stringFilter = $"{Field} mod 2 eq 0";
            }
            else if (SelectedNumOperator == NumericFilterOption.Odd)
            {
                stringFilter = $"{Field} mod 2 eq 1";
            }
            else if (SelectedNumOperator == NumericFilterOption.WholeNumbers)
            {
                stringFilter = $"floor({Field}) eq {Field}";
            }
            else if (SelectedNumOperator == NumericFilterOption.Equal)
            {
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.NotEqual)
            {
                op = ODataOperator.NotEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.GreaterThan)
            {
                op = ODataOperator.GreaterThan;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.GreaterThanOrEqual)
            {
                op = ODataOperator.GreaterThanOrEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.LessThan)
            {
                op = ODataOperator.LessThan;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.LessThanOrEqual)
            {
                op = ODataOperator.LessThanOrEqual;
                value = Value;
            }
            else if (SelectedNumOperator == NumericFilterOption.Between)
            {
                stringFilter = $"{Field} gt {Value} and {Field} lt {Value2}";
            }
            else if (SelectedNumOperator == NumericFilterOption.MultipleOf)
            {
                stringFilter = $"cast({Field}, Edm.Decimal) mod {Value} eq 0";
            }
            else if (SelectedNumOperator == NumericFilterOption.StartsWith)
            {
                op = ODataOperator.StartsWith;
                value = Value.ToString();
                castType = ODataPrimitiveTypes.String;
            }
            else if (SelectedNumOperator == NumericFilterOption.EndsWith)
            {
                op = ODataOperator.EndsWith;
                value = Value.ToString();
                castType = ODataPrimitiveTypes.String;
            }
            else if (SelectedNumOperator == NumericFilterOption.PercentageOf)
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

public class _DateFilter : FilterBase
{
    public DateTime? DateTimeValue { get; set; } = DateTime.Now;
    public DateFilterOption? SelectedDateOperator { get; set; }
    public DateRange? DateRangeValue { get; set; }
    public TimeUnit SelectedTimeUnit { get; set; }
    public int UnitValue { get; set; } = 1;

    public override ODataFilterGenerator ToODataFilter()
    {
        DateTime valueStart = default;
        DateTime valueEnd = default;
        var filter = new ODataFilterGenerator(true, Id);

        if (Operator == ODataOperator.Equal || Operator == ODataOperator.NotEqual)
        {
            //if (SelectedDateOperator == null)
            //{
            //    ClearFilter();
            //    return;
            //}

            (valueStart, valueEnd) = SelectedDateOperator switch
            {
                DateFilterOption.Today => GetLastOrNextDateRange(0),
                DateFilterOption.Torrorrow => GetLastOrNextDateRange(1),
                DateFilterOption.Yesterday => GetLastOrNextDateRange(-1),
                DateFilterOption.Next7Days => GetLastOrNextDateRange(1, 7),
                DateFilterOption.Previous7Days => GetLastOrNextDateRange(-7, 7),
                DateFilterOption.LastWeek => GetLastOrNextDateRange(-1, unit: TimeUnit.Week),
                DateFilterOption.ThisWeek => GetLastOrNextDateRange(0, unit: TimeUnit.Week),
                DateFilterOption.NextWeek => GetLastOrNextDateRange(1, unit: TimeUnit.Week),
                DateFilterOption.LastMonth => GetLastOrNextDateRange(-1, unit: TimeUnit.Month),
                DateFilterOption.ThisMonth => GetLastOrNextDateRange(0, unit: TimeUnit.Month),
                DateFilterOption.NextMonth => GetLastOrNextDateRange(1, unit: TimeUnit.Month),
                DateFilterOption.LastYear => GetLastOrNextDateRange(-1, unit: TimeUnit.Year),
                DateFilterOption.ThisYear => GetLastOrNextDateRange(0, unit: TimeUnit.Year),
                DateFilterOption.NextYear => GetLastOrNextDateRange(1, unit: TimeUnit.Year),
                DateFilterOption.Last => GetLastOrNextDateRange(UnitValue * -1, UnitValue, SelectedTimeUnit),
                DateFilterOption.Next => GetLastOrNextDateRange(1, UnitValue, SelectedTimeUnit),
                DateFilterOption.Date => (DateTimeValue ?? DateTime.Today, (DateTimeValue ?? DateTime.Today).AddDays(1)),
                DateFilterOption.Before => (DateTime.MinValue.ToUniversalTime(), DateTimeValue ?? DateTime.Today),
                DateFilterOption.After => (DateTimeValue ?? DateTime.Today, DateTime.MaxValue.ToUniversalTime()),
                DateFilterOption.Range => (DateRangeValue?.Start ?? DateTime.Today, (DateRangeValue?.End ?? DateTime.Today).AddDays(1)),
                _ => (default, default)
            };

            if (valueStart != default || valueEnd != default)
            {
                var filterStart = new ODataFilter
                {
                    Field = Field,
                    Operator = ODataOperator.GreaterThanOrEqual,
                    Value = valueStart
                };

                var filterEnd = new ODataFilter
                {
                    Field = Field,
                    Operator = ODataOperator.LessThan,
                    Value = valueEnd
                };


                if (Operator == ODataOperator.NotEqual)
                {
                    filterStart.Value = valueEnd;
                    filterEnd.Value = valueStart;
                }

                filter.Add(filterStart).Add(filterEnd);
            }
        }
        else if (Operator == ODataOperator.IsEmpty || Operator == ODataOperator.IsNotEmpty)
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator,
            });
        }

        return filter;
    }

    private (DateTime, DateTime) GetLastOrNextDateRange(int start, int end = 1, TimeUnit unit = TimeUnit.Day)
    {
        DateTime valueStart = default;
        DateTime valueEnd = default;

        switch (unit)
        {
            case TimeUnit.Day:
                valueStart = DateTime.Today.AddDays(start);
                valueEnd = valueStart.AddDays(end);
                break;
            case TimeUnit.Week:
                var startOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                int diff = (7 + (DateTime.Today.DayOfWeek - startOfWeek)) % 7;
                valueStart = DateTime.Today.AddDays(-1 * diff).AddDays(start * 7);
                valueEnd = valueStart.AddDays(7 * end);
                break;
            case TimeUnit.Month:
                valueStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(start);
                valueEnd = valueStart.AddMonths(end);
                break;
            case TimeUnit.Year:
                valueStart = new DateTime(DateTime.Today.Year, 1, 1).AddYears(start);
                valueEnd = valueStart.AddYears(end);
                break;
        }

        return (valueStart, valueEnd);
    }
}