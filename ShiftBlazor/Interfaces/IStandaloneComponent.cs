using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Interfaces;

public interface IStandaloneComponent
{
    public Guid Id { get; }
    public string? Title { get; }
    public string? UniqueName { get; }
    public string IconSvg { get; }
    public bool? ParentReadOnly { get; set; }
    public bool? ParentDisabled { get; set; }
    public string? Height { get; set; }
    public string? NavColor { get; set; }
    public bool NavIconFlatColor { get; set; }
    //public bool Outlined { get; set; }
    //public bool Dense { get; set; }
    //public bool Inline { get; set; }
}
