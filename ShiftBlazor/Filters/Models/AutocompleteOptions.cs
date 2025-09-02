using ShiftSoftware.ShiftBlazor.Interfaces;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class AutocompleteOptions : IODataRequest
{
    public string? EntitySet { get; set; }
    public string? Endpoint { get; set; }
    public string? BaseUrl { get; set; }
    public string? BaseUrlKey { get; set; }
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}