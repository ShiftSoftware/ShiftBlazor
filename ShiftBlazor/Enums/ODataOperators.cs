
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ShiftBlazor.Enums;
public enum ODataOperator
{
    [Display(Name = "ODataFilterEquals", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Equal,
    [Display(Name = "ODataFilterNotEquals", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NotEqual,
    [Display(Name = "ODataFilterGreaterThan", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    GreaterThan,
    [Display(Name = "ODataFilterGreaterThanOrEqual", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    GreaterThanOrEqual,
    [Display(Name = "ODataFilterLessThan", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    LessThan,
    [Display(Name = "ODataFilterLessThanOrEqual", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    LessThanOrEqual,
    [Display(Name = "ODataFilterContains", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Contains,
    [Display(Name = "ODataFilterNotContains", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NotContains,
    [Display(Name = "ODataFilterStartsWith", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    StartsWith,
    [Display(Name = "ODataFilterNotStartsWith", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NotStartsWith,
    [Display(Name = "ODataFilterEndsWith", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    EndsWith,
    [Display(Name = "ODataFilterNotEndsWith", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NotEndsWith,
    [Display(Name = "ODataFilterIn", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    In,
    [Display(Name = "ODataFilterNotIn", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NotIn,
    [Display(Name = "ODataFilterIsEmpty", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    IsEmpty,
    [Display(Name = "ODataFilterIsNotEmpty", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    IsNotEmpty,
}
