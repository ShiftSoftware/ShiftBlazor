using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.RevisionViewer
{
    public class RevisionViewerTests : ShiftBlazorTestContext
    {
        private const string RevisionsEntitySet = "/User/1/revisions";
        private static readonly string ItemUrl = BaseUrl + "/User/1";

        [Fact]
        public void ShouldRenderComponentCorrectly()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters.Add(p => p.EntitySet, RevisionsEntitySet));

            comp.FindComponent<ShiftList<RevisionDTO>>();
        }

        [Fact]
        public void ShouldHideCompareWhenItemUrlNotProvided()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet));

            Assert.False(comp.Instance.CompareEnabled);
            Assert.DoesNotContain("Compare", comp.Markup);
        }

        [Fact]
        public void ShouldShowCompareButtonWhenItemUrlProvided()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl));

            Assert.True(comp.Instance.CompareEnabled);
            Assert.Contains("Compare", comp.Markup);
        }

        [Fact]
        public void SelectionIsPerRow_EvenWhenRevisionIdsAreNull()
        {
            // Regression: RevisionDTO.ID is [JsonIgnore] so all rows have a null ID. Selection must
            // key on ValidFrom, otherwise ticking one row marks every row as selected.
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl));

            var first = new RevisionDTO { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1), ID = null };
            var second = new RevisionDTO { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1), ID = null };

            comp.Instance.SelectedRevisions.Add(first);

            Assert.True(comp.Instance.IsSelected(first));
            Assert.False(comp.Instance.IsSelected(second));
        }

        [Fact]
        public void BuildDiffFlagsChangedFieldsAndOrdersChangedFirst()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl));

            var oldEntity = new JsonObject { ["Name"] = "Ali", ["Status"] = "Active", ["Phone"] = null };
            var newEntity = new JsonObject { ["Name"] = "Alice", ["Status"] = "Active", ["Phone"] = "0770" };

            var rows = comp.Instance.BuildDiff(oldEntity, newEntity);

            // Changed rows are surfaced first.
            Assert.True(rows.First().Changed);

            var name = rows.Single(x => x.Field == "Name");
            Assert.True(name.Changed);
            Assert.Equal("Ali", name.Old);
            Assert.Equal("Alice", name.New);

            Assert.False(rows.Single(x => x.Field == "Status").Changed);

            var phone = rows.Single(x => x.Field == "Phone");
            Assert.True(phone.Changed);
            Assert.Equal("—", phone.Old); // CompareEmptyValue for null/missing
            Assert.Equal("0770", phone.New);
        }

        [Fact]
        public async Task CompareHandlerFetchesBothSnapshotsAndBuildsDiff()
        {
            // Return a different snapshot per as-of timestamp (matched by the year in the query).
            MockHttp.When(HttpMethod.Get, ItemUrl).Respond(req =>
            {
                var query = req.RequestUri?.Query ?? string.Empty;
                var name = query.Contains("2020") ? "OldName" : "NewName";
                var json = System.Text.Json.JsonSerializer.Serialize(
                    new ShiftEntityResponse<Dictionary<string, object>> { Entity = new() { ["Name"] = name } });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                };
            });

            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl));

            comp.Instance.SelectedRevisions.Add(new() { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1), ID = "2" });
            comp.Instance.SelectedRevisions.Add(new() { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1), ID = "1" });

            await comp.InvokeAsync(() => comp.Instance.CompareHandler());

            Assert.True(comp.Instance.ShowCompare);
            var nameRow = comp.Instance.CompareRows.Single(x => x.Field == "Name");
            Assert.True(nameRow.Changed);
            Assert.Equal("OldName", nameRow.Old);
            Assert.Equal("NewName", nameRow.New);
        }
    }
}
