using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Banners;
using ShiftSoftware.ShiftBlazor.Services;
using System.Net;

namespace ShiftSoftware.ShiftBlazor.Tests.Banners;

public class ShiftBannersTests : ShiftBlazorTestContext
{
    private const string RulesJson = """
        {
          // comments and trailing commas are allowed in the file
          "rules": [
            {
              "id": "invoices",
              "paths": [ "/Invoice*", "/InvoiceForm/**" ],
              "severity": "Warning",
              "message": { "en": "Invoices are read-only today.", "ar": "الفواتير للقراءة فقط اليوم." },
            },
            {
              "id": "accountants",
              "paths": [ "/**" ],
              "include": [ "role:accountant" ],
              "exclude": [ "feature:override" ],
              "severity": "Info",
              "message": { "en": "Month-end closing is running." },
            },
          ],
        }
        """;

    private readonly MockedRequest rulesRequest;

    public ShiftBannersTests()
    {
        Services.AddShiftBlazorBanners();
        rulesRequest = MockHttp.When(HttpMethod.Get, "http://localhost/banners.json").Respond("application/json", RulesJson);
    }

    [Fact]
    public void ShowsOnlyTheBannersWhoseRulesMatchThePage()
    {
        NavigateTo("/InvoiceList");

        var cut = Render<ShiftBanners>();

        cut.WaitForAssertion(() => Assert.Equal(["invoices"], BannerIds(cut)));
        Assert.Contains("Invoices are read-only today.", cut.Markup);
        Assert.Contains("mud-alert-text-warning", cut.Markup);
    }

    [Fact]
    public void UpdatesOnNavigation()
    {
        NavigateTo("/InvoiceList");
        var cut = Render<ShiftBanners>();
        cut.WaitForAssertion(() => Assert.Equal(["invoices"], BannerIds(cut)));

        NavigateTo("/ProductList");
        cut.WaitForAssertion(() => Assert.Empty(BannerIds(cut)));

        NavigateTo("/InvoiceForm/12");
        cut.WaitForAssertion(() => Assert.Equal(["invoices"], BannerIds(cut)));
    }

    [Fact]
    public void UsesTheCallersPredicateForIncludeAndExclude()
    {
        NavigateTo("/ProductList");

        var accountant = Render<ShiftBanners>(p => p.Add(x => x.IsConditionMet, c => c == "role:accountant"));
        accountant.WaitForAssertion(() => Assert.Equal(["accountants"], BannerIds(accountant)));

        var overridden = Render<ShiftBanners>(p => p.Add(x => x.IsConditionMet, c => c is "role:accountant" or "feature:override"));
        overridden.WaitForState(() => overridden.Markup.Length >= 0);
        Assert.Empty(BannerIds(overridden));

        var nobody = Render<ShiftBanners>();
        Assert.Empty(BannerIds(nobody));
    }

    [Fact]
    public void ShowsTheUsersLanguage_AndFallsBackToEnglish()
    {
        Services.GetRequiredService<SettingManager>().SwitchLanguage(new() { CultureName = "ar-AE" }, forceReload: false);
        NavigateTo("/InvoiceList");

        var cut = Render<ShiftBanners>(p => p.Add(x => x.IsConditionMet, c => c == "role:accountant"));

        cut.WaitForAssertion(() => Assert.Equal(["invoices", "accountants"], BannerIds(cut)));
        Assert.Contains("الفواتير للقراءة فقط اليوم.", cut.Markup);     // has Arabic
        Assert.Contains("Month-end closing is running.", cut.Markup);   // English only: falls back
    }

    [Fact]
    public void LoadsTheRulesFileOnceForAllBanners()
    {
        NavigateTo("/InvoiceList");

        var first = Render<ShiftBanners>();
        var second = Render<ShiftBanners>();
        first.WaitForAssertion(() => Assert.Single(BannerIds(first)));
        second.WaitForAssertion(() => Assert.Single(BannerIds(second)));

        Assert.Equal(1, MockHttp.GetMatchCount(rulesRequest));
    }

    [Fact]
    public void AMissingRulesFileShowsNoBanners()
    {
        using var context = new FreshContext();
        context.Services.AddShiftBlazorBanners(o => o.RulesPath = "config/missing.json");
        context.Api.When(HttpMethod.Get, "http://localhost/config/missing.json").Respond(HttpStatusCode.NotFound);
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/InvoiceList");

        var cut = context.Render<ShiftBanners>();

        Assert.Empty(BannerIds(cut));
    }

    [Fact]
    public void NullsInTheRulesFileAreSkipped_NotACrash()
    {
        using var context = new FreshContext();
        context.Services.AddShiftBlazorBanners();
        context.Api.When(HttpMethod.Get, "http://localhost/banners.json").Respond("application/json", """
            {
              "rules": [
                null,
                { "id": "null-lists", "paths": null, "include": null, "exclude": null, "message": null },
                {
                  "id": "null-entries",
                  "paths": [ null, "/InvoiceList" ],
                  "include": [ null, "role:accountant" ],
                  "exclude": [ null ],
                  "message": { "ar": null, "en": "Shown." }
                }
              ]
            }
            """);
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/InvoiceList");

        // The predicate would throw if it were asked about a null condition name.
        var cut = context.Render<ShiftBanners>(p => p.Add(x => x.IsConditionMet, c => c.StartsWith("role:") && c == "role:accountant"));

        cut.WaitForAssertion(() => Assert.Equal(["null-entries"], BannerIds(cut)));
        Assert.Contains("Shown.", cut.Markup);
    }

    [Fact]
    public void AnyErrorWhileLoadingTheFileShowsNoBanners()
    {
        using var context = new FreshContext();
        context.Services.AddShiftBlazorBanners();
        context.Api.When(HttpMethod.Get, "http://localhost/banners.json").Throw(new InvalidOperationException("not an HTTP or JSON error"));
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/InvoiceList");

        var cut = context.Render<ShiftBanners>();

        Assert.Empty(BannerIds(cut));
        Assert.False(context.Renderer.UnhandledException.Wait(TimeSpan.FromMilliseconds(500)), "the component crashed");
    }

    [Fact]
    public void AnInvalidRulesPathShowsNoBanners()
    {
        using var context = new FreshContext();
        context.Services.AddShiftBlazorBanners(o => o.RulesPath = "http://[not-a-host]/banners.json");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/InvoiceList");

        var cut = context.Render<ShiftBanners>();

        Assert.Empty(BannerIds(cut));
        Assert.False(context.Renderer.UnhandledException.Wait(TimeSpan.FromMilliseconds(500)), "the component crashed");
    }

    [Fact]
    public void AFailedLoadIsRetriedOnTheNextNavigation_AndASuccessfulOneIsKept()
    {
        using var context = new FreshContext();
        context.Services.AddShiftBlazorBanners();
        var attempts = 0;
        var request = context.Api.When(HttpMethod.Get, "http://localhost/banners.json").Respond(_ => ++attempts == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage { Content = new StringContent(RulesJson, System.Text.Encoding.UTF8, "application/json") });
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/InvoiceList");

        var cut = context.Render<ShiftBanners>();
        Assert.Empty(BannerIds(cut));

        navigation.NavigateTo("/InvoiceForm/12");
        cut.WaitForAssertion(() => Assert.Equal(["invoices"], BannerIds(cut)));

        navigation.NavigateTo("/InvoiceList");
        cut.WaitForAssertion(() => Assert.Equal(["invoices"], BannerIds(cut)));
        Assert.Equal(2, context.Api.GetMatchCount(request));
    }

    [Fact]
    public void WithoutRegistration_ExplainsWhatToCall()
    {
        using var context = new FreshContext();

        var error = Assert.Throws<InvalidOperationException>(() => context.Render<ShiftBanners>());

        Assert.Contains("AddShiftBlazorBanners", error.Message);
    }

    // A context without this class's banner registration and rules file.
    private sealed class FreshContext : ShiftBlazorTestContext
    {
        public MockHttpMessageHandler Api => MockHttp;
    }

    private void NavigateTo(string path) => Services.GetRequiredService<NavigationManager>().NavigateTo(path);

    private static List<string?> BannerIds(IRenderedComponent<ShiftBanners> cut)
        => cut.FindAll("[data-banner-id]").Select(x => x.GetAttribute("data-banner-id")).ToList();
}
