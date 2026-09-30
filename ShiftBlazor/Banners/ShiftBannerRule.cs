using MudBlazor;
using System.Collections.Generic;

namespace ShiftSoftware.ShiftBlazor.Banners;

/// <summary>
/// One banner rule, as read from the rules file (see <see cref="ShiftBannerOptions.RulesPath"/>).
/// <code>
/// {
///   "rules": [
///     {
///       "id": "month-end",
///       "paths": [ "/Invoice*", "/Invoice*/**" ],
///       "include": [ "role:accountant" ],
///       "exclude": [ "feature:closing-override" ],
///       "severity": "Warning",
///       "message": { "en": "Invoices are read-only during month-end closing.", "ar": "..." }
///     }
///   ]
/// }
/// </code>
/// </summary>
public class ShiftBannerRule
{
    /// <summary>Identifies the rule, e.g. in logs. Optional.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// Glob patterns matched against the current page path (see <see cref="ShiftBannerMatcher.IsPathMatch"/>).
    /// The rule applies when any pattern matches. No patterns means the rule never applies.
    /// </summary>
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// Condition names the caller's predicate evaluates. When not empty, at least one must be met.
    /// </summary>
    public List<string> Include { get; set; } = new();

    /// <summary>
    /// Condition names the caller's predicate evaluates. When any is met, the rule does not apply.
    /// Exclude wins over <see cref="Include"/>.
    /// </summary>
    public List<string> Exclude { get; set; } = new();

    /// <summary>How the banner looks. Default <see cref="Severity.Warning"/>.</summary>
    public Severity Severity { get; set; } = Severity.Warning;

    /// <summary>
    /// The text per language, keyed by culture name (<c>ar-IQ</c>) or two-letter language (<c>ar</c>).
    /// <c>en</c> is the fallback (see <see cref="ShiftBannerMatcher.ResolveMessage"/>).
    /// </summary>
    public Dictionary<string, string> Message { get; set; } = new();
}

/// <summary>The root of the rules file: <c>{ "rules": [ ... ] }</c>.</summary>
public class ShiftBannerRuleFile
{
    public List<ShiftBannerRule> Rules { get; set; } = new();
}
