using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Filters;

public enum DateFilterOperator
{
    Today,
    Yesterday,
    Torrorrow,
    Next7Days,
    Previous7Days,
    LastWeek,
    ThisWeek,
    NextWeek,
    LastMonth,
    ThisMonth,
    NextMonth,
    LastYear,
    ThisYear,
    NextYear,
    Last,
    Next,
    Date,
    Before,
    After,
    Range,
}
