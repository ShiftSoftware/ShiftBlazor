
namespace ShiftSoftware.ShiftBlazor.Services;

public class LanguageInfo
{
    public required string CultureName { get; set; }
    public string? Label { get; set; }
    public bool RTL { get; set; }

    /// <summary>
    /// Content-only languages whose text may be left empty: localized inputs skip validation for them
    /// and lay the languages out as tabs.
    /// </summary>
    public bool Optional { get; set; }
}
