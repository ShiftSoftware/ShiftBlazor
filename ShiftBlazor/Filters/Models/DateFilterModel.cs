using System.Globalization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class DateFilterModel : FilterModelBase
{
    public DateTime? DateTimeValue { get; set; } = DateTime.Now;
    public DateFilterOperator? SelectedDateOperator { get; set; }
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
                DateFilterOperator.Last => GetLastOrNextDateRange(UnitValue * -1, UnitValue, SelectedTimeUnit),
                DateFilterOperator.Next => GetLastOrNextDateRange(1, UnitValue, SelectedTimeUnit),
                DateFilterOperator.Date => (DateTimeValue ?? DateTime.Today, (DateTimeValue ?? DateTime.Today).AddDays(1)),
                DateFilterOperator.Before => (DateTime.MinValue.ToUniversalTime(), DateTimeValue ?? DateTime.Today),
                DateFilterOperator.After => (DateTimeValue ?? DateTime.Today, DateTime.MaxValue.ToUniversalTime()),
                DateFilterOperator.Range => (DateRangeValue?.Start ?? DateTime.Today, (DateRangeValue?.End ?? DateTime.Today).AddDays(1)),
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