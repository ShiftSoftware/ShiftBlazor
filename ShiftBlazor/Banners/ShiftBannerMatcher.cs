using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ShiftBlazor.Banners;

/// <summary>
/// Decides which banner rules apply to a page. Pure functions over the rules, the page path and the caller's
/// condition predicate, so the <c>ShiftBanners</c> component holds no matching logic of its own.
/// </summary>
public static class ShiftBannerMatcher
{
    /// <summary>The language used when the rule has no text for the current culture.</summary>
    public const string FallbackLanguage = "en";

    private static readonly ConcurrentDictionary<string, Regex> patternCache = new();

    /// <summary>
    /// Whether <paramref name="path"/> matches the glob <paramref name="pattern"/>. Case-insensitive; the query
    /// string, fragment and a trailing slash of the path are ignored, and both sides are treated as rooted.
    /// <list type="bullet">
    /// <item><c>*</c> matches any characters within one path segment (<c>/Invoice*</c> matches <c>/InvoiceList</c>).</item>
    /// <item><c>?</c> matches one character within a segment.</item>
    /// <item><c>**</c> matches any number of segments, including none: <c>/Invoice/**</c> matches <c>/Invoice</c>,
    /// <c>/Invoice/1</c> and <c>/Invoice/1/lines</c>; <c>/**/edit</c> matches <c>/edit</c> and <c>/a/b/edit</c>.</item>
    /// </list>
    /// </summary>
    public static bool IsPathMatch(string pattern, string path)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        return patternCache.GetOrAdd(NormalizePattern(pattern), ToRegex).IsMatch(NormalizePath(path));
    }

    /// <summary>
    /// Whether <paramref name="rule"/> applies to <paramref name="path"/>: a path pattern matches, and, when the rule
    /// lists <see cref="ShiftBannerRule.Include"/> conditions, at least one is met, and no
    /// <see cref="ShiftBannerRule.Exclude"/> condition is met. <paramref name="isConditionMet"/> decides what a
    /// condition name means; without it no condition is met, so include-rules stay hidden and nothing is excluded.
    /// </summary>
    public static bool IsRuleActive(ShiftBannerRule rule, string path, Func<string, bool>? isConditionMet)
    {
        if (!rule.Paths.Any(pattern => IsPathMatch(pattern, path)))
            return false;

        bool Met(string condition) => isConditionMet?.Invoke(condition) == true;

        if (rule.Include.Count > 0 && !rule.Include.Any(Met))
            return false;

        return !rule.Exclude.Any(Met);
    }

    /// <summary>
    /// The rule's text for <paramref name="culture"/>: its full name (<c>ar-IQ</c>), else its two-letter language
    /// (<c>ar</c>), else <see cref="FallbackLanguage"/>, else <c>null</c>. Keys are compared case-insensitively.
    /// Blank texts count as missing.
    /// </summary>
    public static string? ResolveMessage(IReadOnlyDictionary<string, string>? message, CultureInfo culture)
    {
        if (message is null || message.Count == 0)
            return null;

        foreach (var key in new[] { culture.Name, culture.TwoLetterISOLanguageName, FallbackLanguage })
        {
            var text = message.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    /// <summary>
    /// The banners to show on <paramref name="path"/>, in file order: every active rule that has text for
    /// <paramref name="culture"/> (a rule with no text in that language or in English is skipped).
    /// </summary>
    public static IReadOnlyList<(ShiftBannerRule Rule, string Text)> GetActiveBanners(
        IEnumerable<ShiftBannerRule> rules,
        string path,
        Func<string, bool>? isConditionMet,
        CultureInfo culture)
    {
        var banners = new List<(ShiftBannerRule, string)>();

        foreach (var rule in rules)
        {
            if (!IsRuleActive(rule, path, isConditionMet))
                continue;

            var text = ResolveMessage(rule.Message, culture);
            if (text is not null)
                banners.Add((rule, text));
        }

        return banners;
    }

    // "/invoice/1?tab=lines#top" and "invoice/1/" both become "/invoice/1"; the root stays "/".
    internal static string NormalizePath(string path)
    {
        var end = path.IndexOfAny(new[] { '?', '#' });
        if (end >= 0)
            path = path[..end];

        return "/" + path.Trim().Trim('/');
    }

    // Unlike a path, a pattern keeps '?' (the one-character wildcard) and '#'.
    internal static string NormalizePattern(string pattern) => "/" + pattern.Trim().Trim('/');

    internal static Regex ToRegex(string pattern)
    {
        var regex = new StringBuilder("^");

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
            {
                var atSegmentStart = i > 0 && pattern[i - 1] == '/';
                var atSegmentEnd = i + 2 == pattern.Length || pattern[i + 2] == '/';

                if (atSegmentStart && atSegmentEnd)
                {
                    // "/**" at the end: the path itself or anything below it. "/**/" inside: zero or more segments.
                    regex.Length--; // the "/" already written
                    regex.Append(i + 2 == pattern.Length ? "(/.*)?" : "(/[^/]+)*");
                }
                else
                {
                    regex.Append(".*");
                }

                i++;
            }
            else if (c == '*')
            {
                regex.Append("[^/]*");
            }
            else if (c == '?')
            {
                regex.Append("[^/]");
            }
            else
            {
                regex.Append(Regex.Escape(c.ToString()));
            }
        }

        regex.Append('$');

        // NonBacktracking: patterns come from a config file, so no pattern can make matching slow.
        return new Regex(regex.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    }
}
