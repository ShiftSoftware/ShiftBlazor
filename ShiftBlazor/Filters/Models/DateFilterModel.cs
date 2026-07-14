using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Globalization;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class DateFilterModel : FilterModelBase
{
    private DefaultConverter<DateTime?> _converter = new DefaultConverter<DateTime?>
    {
        Culture = () => CultureInfo.CurrentCulture,
        Format = () => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
    };

    public override bool HasValue()
    {
        (DateTime valueStart, DateTime valueEnd) = GetDateRange(DateFilterOperator.Range, range: Value as DateRange);
        return _HasValue(valueStart, valueEnd);
    }

    private bool _HasValue(DateTime valueStart, DateTime valueEnd)
    {
        return valueStart != default || (valueEnd != default && valueEnd != DateTime.MaxValue.ToUniversalTime());
    }

    public override object? ParseValue(object? obj)
    {
        var str = obj?.ToString();

        if (!string.IsNullOrEmpty(str) && DateRange.TryParse(str, _converter, out var dateRange))
            return dateRange;

        return null;
    }

    public override string? ValueToString()
    {
        if (Value is DateRange dateRange)
            return dateRange.ToString(_converter);

        return Value?.ToString();
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        DateTime valueStart = default;
        DateTime valueEnd = default;
        var filter = new ODataFilterGenerator(true, Id);

        if (Operator == ODataOperator.Equal || Operator == ODataOperator.NotEqual)
        {
            (valueStart, valueEnd) = GetDateRange(DateFilterOperator.Range, range: Value as DateRange);

            if (_HasValue(valueStart, valueEnd))
            {
                var filterStart = new ODataFilter
                {
                    Field = Field,
                    Operator = ODataOperator.GreaterThanOrEqual,
                    Value = valueStart,
                    Prefix = Prefix,
                    IsCollection = IsCollection,
                };

                var filterEnd = new ODataFilter
                {
                    Field = Field,
                    Operator = ODataOperator.LessThan,
                    Value = valueEnd,
                    Prefix = Prefix,
                    IsCollection = IsCollection,
                };

                if (Operator == ODataOperator.NotEqual)
                {
                    filterStart.Value = valueEnd;
                    filterEnd.Value = valueStart;
                }

                filter.Add(filterStart).Add(filterEnd);
            }
        }
        else if (IsNoValueOperator)
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator,
                Prefix = Prefix,
                IsCollection = IsCollection,
            });
        }

        return filter;
    }

    public static (DateTime dateStart, DateTime dateEnd) GetDateRange(DateFilterOperator? dateOperator, TimeUnit timeUnit = TimeUnit.Day, int unitValue = 1, DateTime? date = null, DateRange? range = null)
    {
        (DateTime valueStart, DateTime valueEnd) = dateOperator switch
        {
            DateFilterOperator.Today => GetLastOrNextDateRange(0),
            DateFilterOperator.Torrorrow => GetLastOrNextDateRange(1),
            DateFilterOperator.Yesterday => GetLastOrNextDateRange(-1),
            DateFilterOperator.Next7Days => GetLastOrNextDateRange(1, 7),
            DateFilterOperator.Previous7Days => GetLastOrNextDateRange(-7, 7),
            DateFilterOperator.LastWeek => GetLastOrNextDateRange(-1, unit: TimeUnit.Week),
            DateFilterOperator.ThisWeek => GetLastOrNextDateRange(0, unit: TimeUnit.Week),
            DateFilterOperator.NextWeek => GetLastOrNextDateRange(1, unit: TimeUnit.Week),
            DateFilterOperator.LastMonth => GetLastOrNextDateRange(-1, unit: TimeUnit.Month),
            DateFilterOperator.ThisMonth => GetLastOrNextDateRange(0, unit: TimeUnit.Month),
            DateFilterOperator.NextMonth => GetLastOrNextDateRange(1, unit: TimeUnit.Month),
            DateFilterOperator.LastYear => GetLastOrNextDateRange(-1, unit: TimeUnit.Year),
            DateFilterOperator.ThisYear => GetLastOrNextDateRange(0, unit: TimeUnit.Year),
            DateFilterOperator.NextYear => GetLastOrNextDateRange(1, unit: TimeUnit.Year),
            DateFilterOperator.Last => GetLastOrNextDateRange(unitValue * -1, unitValue, timeUnit),
            DateFilterOperator.Next => GetLastOrNextDateRange(1, unitValue, timeUnit),
            DateFilterOperator.Date => (date ?? DateTime.Today, (date ?? DateTime.Today).AddDays(1)),
            DateFilterOperator.Before => (DateTime.MinValue.ToUniversalTime(), date ?? DateTime.Today),
            DateFilterOperator.After => (date ?? DateTime.Today, DateTime.MaxValue.ToUniversalTime()),
            DateFilterOperator.Range => (range?.Start ?? DateTime.MinValue.ToUniversalTime(), range?.End?.AddDays(1) ?? DateTime.MaxValue.ToUniversalTime()),
            _ => (default, default)
        };

        return (valueStart, valueEnd);
    }

    public static (DateTime, DateTime) GetLastOrNextDateRange(int start, int end = 1, TimeUnit unit = TimeUnit.Day)
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