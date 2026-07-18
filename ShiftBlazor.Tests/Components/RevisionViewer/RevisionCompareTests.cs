using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Tests;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.RevisionViewer
{
    public class RevisionCompareTests : ShiftBlazorTestContext
    {
        private static readonly string ItemUrl = BaseUrl + "/User/1";

        // Renders the value's Name so we can assert each host bound to its own snapshot.
        private static readonly RenderFragment<FormChildContext<SampleDTO>> NameContent =
            context => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, context.Item.Name);
                builder.CloseElement();
            };

        private void MockSnapshots(string oldName, string newName, string city = "Basra")
        {
            // The two snapshots are told apart by the as-of year in the query string.
            MockHttp.When(HttpMethod.Get, ItemUrl).Respond(req =>
            {
                var query = req.RequestUri?.Query ?? string.Empty;
                var entity = query.Contains("2020")
                    ? new SampleDTO { Name = oldName, City = city }
                    : new SampleDTO { Name = newName, City = city };

                var json = JsonSerializer.Serialize(
                    new ShiftEntityResponse<SampleDTO> { Entity = entity },
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                };
            });
        }

        private IRenderedComponent<RevisionCompare<SampleDTO>> RenderCompare()
        {
            var older = new RevisionDTO { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1) };
            var newer = new RevisionDTO { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1) };

            var comp = RenderComponent<RevisionCompare<SampleDTO>>(parameters => parameters
                .Add(p => p.ItemUrl, ItemUrl)
                .Add(p => p.OldRevision, older)
                .Add(p => p.NewRevision, newer)
                .Add(p => p.ChildContent, NameContent));

            comp.WaitForState(() => !comp.Instance.Loading);
            return comp;
        }

        [Fact]
        public void RendersEachRevisionWithTheFormFields()
        {
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            // Both snapshots are rendered through the supplied ChildContent (the real form fields).
            Assert.Contains("Ali", comp.Markup);
            Assert.Contains("Alice", comp.Markup);
        }

        [Fact]
        public void ChangedFieldsBannerListsOnlyDifferingProperties()
        {
            MockSnapshots("Ali", "Alice"); // Name differs, City is the same

            var comp = RenderCompare();

            Assert.Contains("Name", comp.Instance.ChangedFields);
            Assert.DoesNotContain("City", comp.Instance.ChangedFields);
        }

        [Fact]
        public void NoChangedFieldsWhenSnapshotsAreIdentical()
        {
            MockSnapshots("Ali", "Ali"); // identical

            var comp = RenderCompare();

            Assert.Empty(comp.Instance.ChangedFields);
        }

        [Fact]
        public void RegistersAsTopShortcutComponentSoEscapeTargetsThisDialog()
        {
            // Regression: without registering, Escape would close the revisions list behind instead of the compare.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            Assert.IsType<RevisionCompare<SampleDTO>>(comp.Instance);
            Assert.Same(comp.Instance, IShortcutComponent.GetComponent(^1));
        }
    }
}
