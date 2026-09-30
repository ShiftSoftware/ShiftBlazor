using ShiftSoftware.ShiftBlazor.Banners;
using System.Globalization;

namespace ShiftSoftware.ShiftBlazor.Tests.Banners;

public class ShiftBannerMatcherTests
{
    [Theory]
    // Literal paths: case-insensitive, leading/trailing slash, query and fragment ignored.
    [InlineData("/InvoiceList", "/InvoiceList", true)]
    [InlineData("/InvoiceList", "/invoicelist", true)]
    [InlineData("InvoiceList", "/InvoiceList/", true)]
    [InlineData("/InvoiceList", "/InvoiceList?tab=open#top", true)]
    [InlineData("/InvoiceList", "/InvoiceListing", false)]
    [InlineData("/", "/", true)]
    [InlineData("/", "/InvoiceList", false)]
    // * stays inside one segment.
    [InlineData("/Invoice*", "/InvoiceList", true)]
    [InlineData("/Invoice*", "/Invoice", true)]
    [InlineData("/Invoice*", "/InvoiceForm/12", false)]
    [InlineData("/*/edit", "/Invoice/edit", true)]
    [InlineData("/*/edit", "/Invoice/12/edit", false)]
    // ? is exactly one character, never a slash.
    [InlineData("/Form?", "/FormA", true)]
    [InlineData("/Form?", "/Form", false)]
    [InlineData("/Form?", "/FormAB", false)]
    [InlineData("/Form?/x", "/Form//x", false)]
    // /** at the end: the path itself and everything below it.
    [InlineData("/InvoiceForm/**", "/InvoiceForm", true)]
    [InlineData("/InvoiceForm/**", "/InvoiceForm/12", true)]
    [InlineData("/InvoiceForm/**", "/InvoiceForm/12/lines", true)]
    [InlineData("/InvoiceForm/**", "/InvoiceFormX", false)]
    [InlineData("/**", "/", true)]
    [InlineData("/**", "/anything/at/all", true)]
    // /**/ inside: zero or more whole segments.
    [InlineData("/**/edit", "/edit", true)]
    [InlineData("/**/edit", "/a/b/edit", true)]
    [InlineData("/**/edit", "/a/b/editor", false)]
    [InlineData("/admin/**/logs", "/admin/logs", true)]
    [InlineData("/admin/**/logs", "/admin/x/y/logs", true)]
    // ** glued to other text crosses segments.
    [InlineData("/report**", "/reports/2026/09", true)]
    // Regex characters in a pattern are literal.
    [InlineData("/a.b", "/a.b", true)]
    [InlineData("/a.b", "/axb", false)]
    [InlineData("/(x)", "/(x)", true)]
    // Blank patterns never match.
    [InlineData("", "/", false)]
    [InlineData("   ", "/InvoiceList", false)]
    public void IsPathMatch_FollowsTheGlobRules(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, ShiftBannerMatcher.IsPathMatch(pattern, path));
    }

    [Fact]
    public void IsRuleActive_NeedsAPathMatch()
    {
        var rule = Rule(paths: ["/InvoiceList"]);

        Assert.True(ShiftBannerMatcher.IsRuleActive(rule, "/InvoiceList", null));
        Assert.False(ShiftBannerMatcher.IsRuleActive(rule, "/ProductList", null));
        Assert.False(ShiftBannerMatcher.IsRuleActive(Rule(paths: []), "/InvoiceList", null));
    }

    [Fact]
    public void IsRuleActive_IncludeNeedsAtLeastOneConditionMet()
    {
        var rule = Rule(paths: ["/**"], include: ["role:accountant", "role:auditor"]);

        Assert.True(ShiftBannerMatcher.IsRuleActive(rule, "/x", c => c == "role:auditor"));
        Assert.False(ShiftBannerMatcher.IsRuleActive(rule, "/x", c => c == "role:sales"));
    }

    [Fact]
    public void IsRuleActive_ExcludeWinsOverInclude()
    {
        var rule = Rule(paths: ["/**"], include: ["role:accountant"], exclude: ["feature:override"]);

        Assert.True(ShiftBannerMatcher.IsRuleActive(rule, "/x", c => c == "role:accountant"));
        Assert.False(ShiftBannerMatcher.IsRuleActive(rule, "/x", c => c is "role:accountant" or "feature:override"));
    }

    [Fact]
    public void IsRuleActive_WithoutAPredicate_IncludeRulesStayHiddenAndNothingIsExcluded()
    {
        Assert.False(ShiftBannerMatcher.IsRuleActive(Rule(paths: ["/**"], include: ["role:accountant"]), "/x", null));
        Assert.True(ShiftBannerMatcher.IsRuleActive(Rule(paths: ["/**"], exclude: ["feature:override"]), "/x", null));
    }

    [Fact]
    public void IsRuleActive_AsksThePredicateOnlyAboutTheRulesConditions()
    {
        var asked = new List<string>();
        var rule = Rule(paths: ["/**"], include: ["a"], exclude: ["b"]);

        ShiftBannerMatcher.IsRuleActive(rule, "/x", c => { asked.Add(c); return c == "a"; });

        Assert.Equal(["a", "b"], asked);
    }

    [Theory]
    [InlineData("ar-IQ", "Iraqi Arabic")]   // exact culture name first
    [InlineData("ar-SA", "Arabic")]         // then the two-letter language
    [InlineData("ku", "Kurdish")]
    [InlineData("tr-TR", "English")]        // then English
    [InlineData("en-GB", "English")]
    public void ResolveMessage_PrefersCultureThenLanguageThenEnglish(string culture, string expected)
    {
        var message = new Dictionary<string, string>
        {
            ["en"] = "English",
            ["ar"] = "Arabic",
            ["ar-IQ"] = "Iraqi Arabic",
            ["KU"] = "Kurdish",   // keys are case-insensitive
        };

        Assert.Equal(expected, ShiftBannerMatcher.ResolveMessage(message, new CultureInfo(culture)));
    }

    [Fact]
    public void ResolveMessage_BlankTextFallsBack_AndNoTextAnywhereIsNull()
    {
        var culture = new CultureInfo("ar");

        Assert.Equal("English", ShiftBannerMatcher.ResolveMessage(new Dictionary<string, string> { ["ar"] = "  ", ["en"] = "English" }, culture));
        Assert.Null(ShiftBannerMatcher.ResolveMessage(new Dictionary<string, string> { ["tr"] = "Turkish" }, culture));
        Assert.Null(ShiftBannerMatcher.ResolveMessage(new Dictionary<string, string>(), culture));
        Assert.Null(ShiftBannerMatcher.ResolveMessage(null, culture));
    }

    [Fact]
    public void GetActiveBanners_KeepsFileOrder_AndSkipsRulesWithNoTextToShow()
    {
        var first = Rule(paths: ["/Invoice*"], message: new() { ["en"] = "first" });
        var elsewhere = Rule(paths: ["/Product*"], message: new() { ["en"] = "elsewhere" });
        var noEnglish = Rule(paths: ["/**"], message: new() { ["tr"] = "yalnızca Türkçe" });
        var second = Rule(paths: ["/**"], message: new() { ["en"] = "second", ["ar"] = "الثاني" });

        var banners = ShiftBannerMatcher.GetActiveBanners([first, elsewhere, noEnglish, second], "/InvoiceList", null, new CultureInfo("ar"));

        Assert.Equal(["first", "الثاني"], banners.Select(b => b.Text));
        Assert.Same(first, banners[0].Rule);
        Assert.Same(second, banners[1].Rule);
    }

    private static ShiftBannerRule Rule(
        List<string>? paths = null,
        List<string>? include = null,
        List<string>? exclude = null,
        Dictionary<string, string>? message = null) => new()
    {
        Paths = paths ?? [],
        Include = include ?? [],
        Exclude = exclude ?? [],
        Message = message ?? new() { ["en"] = "text" },
    };
}
