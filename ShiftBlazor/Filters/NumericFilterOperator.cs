using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Filters;

public enum NumericFilterOperator
{
    Positive,
    Negative,
    Even,
    Odd,
    WholeNumbers,
    Equal,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Between,
    MultipleOf,
    EndsWith,
    StartsWith,
    PercentageOf,
}
