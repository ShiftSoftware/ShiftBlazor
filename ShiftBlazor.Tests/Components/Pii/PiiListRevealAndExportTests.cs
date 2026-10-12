using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using Xunit;
using ContactDTO = ShiftSoftware.ShiftBlazor.Tests.Components.Pii.PiiColumnList.ContactDTO;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Pii;

public class PiiListRevealAndExportTests : ShiftBlazorTestContext
{
    private readonly List<string> reveals = new();

    public PiiListRevealAndExportTests()
    {
        Services.AddShiftEntityPii();
        Grant(reveal: true);
        MockHttp.When(HttpMethod.Post, BaseUrl + "/*").Respond(request =>
        {
            reveals.Add(request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PiiRevealDTO { Value = "synthetic-raw-" + reveals.Count })
            };
        });
    }

    private void Grant(bool reveal)
    {
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder()
            .AddAccessTree(reveal ? "{\"PiiActionTree\":{\"Reveal\":[\"m\"]}}" : "{}")
            .AddActionTree<PiiActionTree>()
            .Build());
    }

    private static List<ContactDTO> Rows() =>
    [
        new()
        {
            ID = "7", OwnerID = "o-9", Label = "first",
            Phone = new() { Display = "•••• 0088" },
            Secret = new() { Display = "•••• 12" },
            Alias = new() { Display = "•••• 0055" },
            Name = new() { Display = "J•••" }
        },
        new() { ID = "8", Label = "second" }
    ];

    [Fact]
    public void Export_metadata_declares_the_mask_for_every_protected_column_however_it_is_bound()
    {
        var cut = Render<PiiColumnList>(p => p.Add(x => x.Rows, Rows()));
        var list = cut.FindComponent<ShiftList<ContactDTO>>().Instance;

        var columns = list.BuildExportPayload().Columns.ToDictionary(x => x.Key);

        Assert.Null(columns["Label"].ValueMember);
        foreach (var key in new[] { "Phone", "Secret", "Alias", "Name" })
            Assert.Equal(nameof(PiiFieldDTO.Display), columns[key].ValueMember);
        Assert.Equal(nameof(PiiFieldDTO.Display), columns["Label"].CustomColumn!.ArgValueMembers!["Phone"]);
        Assert.False(columns["Label"].CustomColumn!.ArgValueMembers!.ContainsKey("Label"));
    }

    [Fact]
    public void Collections_of_a_declared_type_export_the_declared_member()
    {
        Assert.Equal("Display", ShiftList<ContactDTO>.GetExportValueMember(typeof(List<PiiFieldDTO>)));
        Assert.Equal("Display", ShiftList<ContactDTO>.GetExportValueMember(typeof(PiiFieldDTO[])));
        Assert.Null(ShiftList<ContactDTO>.GetExportValueMember(typeof(string)));
        Assert.Null(ShiftList<ContactDTO>.GetExportValueMember(typeof(List<string>)));
    }

    [Fact]
    public void Cells_show_the_mask_and_reveal_into_the_cell_only()
    {
        var rows = Rows();
        var cut = Render<PiiColumnList>(p => p.Add(x => x.Rows, rows));

        cut.WaitForAssertion(() => Assert.Contains("•••• 0088", cut.Markup));
        // Phone and Alias on the first row; Secret is Revealable=false; the second row has no wrappers.
        var buttons = cut.FindAll("button[aria-label^='Reveal ']");
        Assert.Equal(["Reveal Phone", "Reveal Alias"], buttons.Select(b => b.GetAttribute("aria-label")));

        cut.Find("button[aria-label='Reveal Phone']").Click();
        cut.WaitForAssertion(() => Assert.Contains("synthetic-raw-1", cut.Markup));
        Assert.Equal("/protected-contacts/7/pii/Phone/reveal", reveals.Single());
        Assert.Equal("•••• 0088", rows[0].Phone!.Display);
        Assert.Null(rows[0].Phone!.Value);

        cut.Find("button[aria-label='Reveal Alias']").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, reveals.Count));
        Assert.Equal("/owners/o-9/pii/OwnerPhone/reveal", reveals[1]);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("button[aria-label^='Reveal ']")));
    }

    [Fact]
    public void Protected_column_keeps_the_query_rules_of_a_display_bound_column()
    {
        var cut = Render<PiiColumnList>(p => p.Add(x => x.Rows, Rows()));
        var phone = cut.FindComponents<ShiftPiiColumn<ContactDTO>>().Single(x => x.Instance.Title == "Phone").Instance;
        var secret = cut.FindComponents<ShiftPiiColumn<ContactDTO>>().Single(x => x.Instance.Title == "Secret").Instance;

        Assert.False(phone.Sortable);
        Assert.True(phone.Filterable);
        Assert.Equal([MudBlazor.FilterOperator.String.Equal], phone.FilterOperators);
        Assert.False(secret.Filterable);
    }

    [Fact]
    public void Without_the_reveal_grant_cells_are_masks_only()
    {
        Grant(reveal: false);
        var cut = Render<PiiColumnList>(p => p.Add(x => x.Rows, Rows()));

        cut.WaitForAssertion(() => Assert.Contains("•••• 0088", cut.Markup));
        Assert.Empty(cut.FindAll("button[aria-label^='Reveal ']"));
    }

    [Fact]
    public void Text_resets_when_reused_for_another_record_and_reports_a_refusal()
    {
        MockHttp.Clear();
        MockHttp.When(HttpMethod.Post, BaseUrl + "/contacts/1/pii/Phone/reveal").Respond(HttpStatusCode.Forbidden);
        MockHttp.When(HttpMethod.Post, BaseUrl + "/contacts/2/pii/Phone/reveal")
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PiiRevealDTO { Value = "synthetic-raw-2" }) });

        var cut = Render<ShiftPiiText>(p => p
            .Add(x => x.Field, new PiiFieldDTO { Display = "•• 1" })
            .Add(x => x.Endpoint, "contacts").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone").Add(x => x.Label, "Phone"));

        Assert.Equal("Reveal Phone", cut.Find("button").GetAttribute("aria-label"));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal("Not allowed to reveal", cut.Find("[role=alert]").TextContent));
        Assert.Contains("•• 1", cut.Markup);

        cut.Render(p => p.Add(x => x.Field, new PiiFieldDTO { Display = "•• 2" }).Add(x => x.RecordKey, "2"));
        Assert.Empty(cut.FindAll("[role=alert]"));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("synthetic-raw-2", cut.Markup));

        cut.Render(p => p.Add(x => x.Field, new PiiFieldDTO { Display = "•• 3" }).Add(x => x.RecordKey, "3"));
        Assert.DoesNotContain("synthetic-raw-2", cut.Markup);
        Assert.Contains("•• 3", cut.Markup);
        Assert.NotNull(cut.Find("button"));
    }

    [Fact]
    public void Text_without_a_record_or_with_reveal_disallowed_has_no_button()
    {
        var field = new PiiFieldDTO { Display = "•• 1" };
        Assert.Empty(Render<ShiftPiiText>(p => p.Add(x => x.Field, field).Add(x => x.Endpoint, "c").Add(x => x.Member, "Phone")).FindAll("button"));
        Assert.Empty(Render<ShiftPiiText>(p => p.Add(x => x.Field, field).Add(x => x.Endpoint, "c").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone").Add(x => x.AllowReveal, false)).FindAll("button"));
        Assert.Empty(Render<ShiftPiiText>(p => p.Add(x => x.Endpoint, "c").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone")).FindAll("button"));
        // No mask means no stored value, so there is nothing to reveal.
        Assert.Empty(Render<ShiftPiiText>(p => p.Add(x => x.Field, new PiiFieldDTO { Display = "" }).Add(x => x.Endpoint, "c").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone")).FindAll("button"));
        // Unlabelled text still names the action.
        Assert.Equal("Reveal", Render<ShiftPiiText>(p => p.Add(x => x.Field, field).Add(x => x.Endpoint, "c").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone")).Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Text_reports_a_failed_reveal_apart_from_a_refused_grant()
    {
        MockHttp.Clear();
        MockHttp.When(HttpMethod.Post, BaseUrl + "/contacts/1/pii/Phone/reveal").Respond(HttpStatusCode.InternalServerError);
        var cut = Render<ShiftPiiText>(p => p
            .Add(x => x.Field, new PiiFieldDTO { Display = "•• 1" })
            .Add(x => x.Endpoint, "contacts").Add(x => x.RecordKey, "1").Add(x => x.Member, "Phone"));

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal("Could not reveal", cut.Find("[role=alert]").TextContent));
        Assert.NotNull(cut.Find("button"));
    }
}
