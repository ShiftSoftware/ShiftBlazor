using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.UI;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Pii;

public class PiiSearchListTests : ShiftBlazorTestContext
{
    public static TheoryData<bool, bool, bool, bool> PermissionCases
    {
        get
        {
            var cases = new TheoryData<bool, bool, bool, bool>();
            foreach (var replaceReveal in new[] { false, true })
            foreach (var replaceSearch in new[] { false, true })
            foreach (var reveal in new[] { false, true })
            foreach (var search in new[] { false, true })
                cases.Add(replaceReveal, replaceSearch, reveal, search);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(PermissionCases))]
    public async Task Column_panel_and_reveal_control_use_independent_permissions(bool replaceReveal, bool replaceSearch, bool reveal, bool partial)
    {
        Services.AddShiftEntityPii(o =>
        {
            if (replaceReveal) o.Action = HostPiiActions.Reveal;
            if (replaceSearch) o.PartialSearchAction = HostPiiActions.PartialSearch;
        });
        Services.RemoveAll<ITypeAuthService>();
        // Give the unused default/override the opposite grant to catch accidental OR checks.
        var tree = new Dictionary<string, object>
        {
            [nameof(PiiActionTree)] = new
            {
                Reveal = (replaceReveal ? !reveal : reveal) ? new[] { "m" } : [],
                PartialSearch = (replaceSearch ? !partial : partial) ? new[] { "m" } : []
            },
            [nameof(HostPiiActions)] = new
            {
                Reveal = (replaceReveal ? reveal : !reveal) ? new[] { "m" } : [],
                PartialSearch = (replaceSearch ? partial : !partial) ? new[] { "m" } : []
            }
        };
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder().AddActionTree<PiiActionTree>().AddActionTree<HostPiiActions>()
            .AddAccessTree(System.Text.Json.JsonSerializer.Serialize(tree)).Build());
        var requests = new List<string>();
        MockHttp.When(HttpMethod.Get, BaseUrl + "/protected-contacts").Respond(request =>
        {
            requests.Add(Uri.UnescapeDataString(request.RequestUri!.Query));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[],\"@odata.count\":0}", System.Text.Encoding.UTF8, "application/json")
            };
        });
        var cut = Render<PiiSearchList>();
        var list = cut.FindComponent<ShiftList<PiiSearchList.ContactDTO>>().Instance;
        var phone = cut.FindComponents<PropertyColumnExtended<PiiSearchList.ContactDTO, string>>()
            .Single(x => x.Instance.Title == "Phone");
        var secret = cut.FindComponents<PropertyColumnExtended<PiiSearchList.ContactDTO, string>>()
            .Single(x => x.Instance.Title == "Secret");
        Assert.False(phone.Instance.Sortable);
        Assert.False(secret.Instance.Filterable);
        Assert.Equal(partial, phone.Instance.FilterOperators.Contains(FilterOperator.String.Contains));
        Assert.Contains(FilterOperator.String.Equal, phone.Instance.FilterOperators);
        var panel = cut.FindComponents<StringFilterUI>().Single(x => x.Instance.Filter.Field == "Phone/Display");
        var model = panel.Instance.Filter;
        Assert.Equal(partial ? ODataOperator.Contains : ODataOperator.Equal, model.Operator);
        Assert.Equal(partial ? 4 : 1, model.AllowedOperators!.Count);
        Assert.Equal(!partial, panel.FindComponent<MudSelect<ODataOperator>>().Instance.Disabled);
        // Restored filters obey today's permission, too.
        model.Operator = ODataOperator.Contains;
        model.Value = "07500000088";
        await cut.InvokeAsync(list.Reload);
        cut.WaitForAssertion(() => Assert.Contains(requests, q => partial
            ? q.Contains("contains(Phone/Display,'07500000088')") : q.Contains("Phone/Display eq '07500000088'")));
        model.Value = null;
        await cut.InvokeAsync(async () => await list.DataGrid!.AddFilterAsync(new FilterDefinition<PiiSearchList.ContactDTO>
        {
            Column = phone.Instance,
            Operator = FilterOperator.String.Contains,
            Value = "07500000099"
        }));
        cut.WaitForAssertion(() => Assert.Contains(requests, q => partial
            ? q.Contains("contains(Phone/Display,'07500000099')") : q.Contains("Phone/Display eq '07500000099'")));

        var field = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts").Add(p => p.RecordKey, "7").Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));
        Assert.Equal(reveal ? 1 : 0, field.FindAll("button").Count);
        Assert.NotNull(field.Find("input").GetAttribute("readonly"));
    }

    [ActionTree("Host protected information", "Synthetic application override")]
    public class HostPiiActions
    {
        public static readonly BooleanAction Reveal = new("Reveal or change protected information");
        public static readonly BooleanAction PartialSearch = new("Search protected information using partial values");
    }
}
