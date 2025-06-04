using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ShiftBlazor.Filters;

public enum DateFilterOperator
{
    [Display(Name = "Today", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Today,

    [Display(Name = "Yesterday", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Yesterday,

    [Display(Name = "Tomorrow", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Torrorrow,

    [Display(Name = "Next7Days", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Next7Days,

    [Display(Name = "Previous7Days", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Previous7Days,

    [Display(Name = "LastWeek", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    LastWeek,

    [Display(Name = "ThisWeek", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    ThisWeek,

    [Display(Name = "NextWeek", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NextWeek,

    [Display(Name = "LastMonth", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    LastMonth,

    [Display(Name = "ThisMonth", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    ThisMonth,

    [Display(Name = "NextMonth", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NextMonth,

    [Display(Name = "LastYear", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    LastYear,

    [Display(Name = "ThisYear", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    ThisYear,

    [Display(Name = "NextYear", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    NextYear,

    [Display(Name = "Last", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Last,

    [Display(Name = "Next", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Next,

    [Display(Name = "Date", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Date,

    [Display(Name = "Before", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Before,

    [Display(Name = "After", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    After,

    [Display(Name = "Range", ResourceType = typeof(ShiftSoftwareLocalization.Blazor.Resource))]
    Range,
}
