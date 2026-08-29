using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftForm;

/// <summary>
/// Opening one revision through the form's own URL — the link behind "Open In New Tab", and what
/// makes two revisions readable side by side in two browser tabs.
/// </summary>
public class AsOfDeepLinkTests : ShiftBlazorTestContext
{
    // Not "Product": ShiftBlazorTestContext already mocks Product/1, and two handlers for one URL
    // make which of them answers a matter of registration order rather than of the test.
    private const string Endpoint = "City";
    private static readonly string ItemUrl = BaseUrl + "/City/1";

    // The deep-link path reads the live record and then the revision, two sequential requests.
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private static readonly DateTimeOffset Revision =
        new(2023, 12, 16, 14, 23, 43, TimeSpan.FromHours(3));

    /// <summary>Records the query of every read of the record, and answers with a per-query name.</summary>
    private List<string> MockRecord()
    {
        var requested = new List<string>();

        MockHttp.When(HttpMethod.Get, ItemUrl).Respond(req =>
        {
            var query = req.RequestUri?.Query ?? string.Empty;
            requested.Add(query);

            var json = JsonSerializer.Serialize(
                new ShiftEntityResponse<SampleDTO>
                {
                    Entity = new SampleDTO { ID = "1", Name = query.Contains("asOf") ? "Revision" : "Live" },
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        return requested;
    }

    private IRenderedComponent<ShiftEntityForm<SampleDTO>> RenderAt(string url)
    {
        Services.GetRequiredService<FakeNavigationManager>().NavigateTo(url);

        return RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Endpoint, Endpoint));
    }

    [Fact]
    public void TheLinkFormatSurvivesTheQueryStringRoundTrip()
    {
        // Regression: DateTimeOffset.ToString("O") emits "+00:00" even in UTC, and a raw '+' in a
        // query decodes back as a space — an unparseable timestamp, so the form silently showed the
        // live record. The 'Z' form carries no character the query string will damage.
        var formatted = ShiftEntityForm<SampleDTO>.FormatAsOf(Revision);

        Assert.DoesNotContain("+", formatted);
        Assert.EndsWith("Z", formatted);

        var roundTripped = System.Web.HttpUtility
            .ParseQueryString("?asOf=" + formatted)
            .Get("asOf");

        Assert.Equal(Revision, DateTimeOffset.Parse(roundTripped!));
    }

    [Fact]
    public void FormOpenedAtAnAsOfUrlLoadsThatRevision()
    {
        var requested = MockRecord();

        var comp = RenderAt("City/1?asOf=" + ShiftEntityForm<SampleDTO>.FormatAsOf(Revision));

        // The mode is set after the value arrives, so both belong inside the wait — asserting the
        // mode outside it races the continuation that sets it.
        comp.WaitForAssertion(() =>
        {
            Assert.Equal("Revision", comp.Instance.Value.Name);
            Assert.Equal(FormModes.Archive, comp.Instance.Mode);
        }, Wait);

        // Live first (that is what closing the revision restores to), then the revision itself.
        Assert.Contains(requested, q => q == string.Empty);
        Assert.Contains(requested, q => q.Contains("asOf"));
    }

    [Fact]
    public void AnAsOfUrlWhosePlusBecameASpaceStillLoadsTheRevision()
    {
        // Hand-typed links, and links written before the 'Z' form, arrive damaged this way.
        var requested = MockRecord();

        var comp = RenderAt("City/1?asOf=2023-12-16T11:23:43.0000000+00:00");

        comp.WaitForAssertion(() => Assert.Equal("Revision", comp.Instance.Value.Name), Wait);
        Assert.Contains(requested, q => q.Contains("asOf"));
    }

    [Fact]
    public async Task NewTabUrlKeepsTheComponentsOwnRoutePrefix()
    {
        // Regression: the URL was built from the component's type name, so a form whose route
        // carries a prefix ("Identity/CityForm") opened at "CityForm" — a route that isn't there.
        // It was also relative, which loses the base path when the app is hosted under one.
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("Cities");

        var modal = Services.GetRequiredService<ShiftSoftware.ShiftBlazor.Services.ShiftModal>();

        await modal.Open(
            typeof(PrefixedForm),
            "VXwB3",
            ModalOpenMode.NewTab,
            new Dictionary<string, object> { ["asOf"] = ShiftEntityForm<SampleDTO>.FormatAsOf(Revision) });

        var url = (string)JSInterop.Invocations.Single(x => x.Identifier == "open").Arguments[0]!;

        Assert.StartsWith(nav.BaseUri, url);
        Assert.Contains("Identity/PrefixedForm/VXwB3", url);
        Assert.Contains("asOf=", url);
    }

    /// <summary>A form whose route sits under a prefix, as ShiftIdentity's forms do.</summary>
    [Microsoft.AspNetCore.Components.Route("/Identity/PrefixedForm/{Key?}")]
    private sealed class PrefixedForm : Microsoft.AspNetCore.Components.ComponentBase
    {
    }

    [Fact]
    public void FormWithNoAsOfLoadsTheLiveRecord()
    {
        var requested = MockRecord();

        var comp = RenderAt("City/1");

        comp.WaitForAssertion(() => Assert.Equal("Live", comp.Instance.Value.Name), Wait);

        Assert.All(requested, q => Assert.Equal(string.Empty, q));
        Assert.Equal(FormModes.View, comp.Instance.Mode);
    }
}
