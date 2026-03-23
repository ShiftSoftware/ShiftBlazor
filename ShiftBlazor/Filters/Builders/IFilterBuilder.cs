using ShiftSoftware.ShiftBlazor.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Filters.Builders;

public interface IFilterBuilder
{
    public IFilterableComponent? Parent { get; set; }
    public void Build();

}
