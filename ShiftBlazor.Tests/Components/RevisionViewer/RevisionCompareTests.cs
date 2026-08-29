using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Tests;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.RevisionViewer
{
    public class RevisionCompareTests : ShiftBlazorTestContext
    {
        private const string Endpoint = "User";
        private static readonly string ItemUrl = BaseUrl + "/User/1";

        // Binds through the form it is in, which is what lets one fragment render two revisions.
        private static readonly RenderFragment<FormChildContext<SampleDTO>> NameContent =
            context => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, context.Item.Name);
                builder.CloseElement();
            };

        // The revision each pane is pinned to is told apart by the as-of year in the query string.
        private List<string> MockSnapshots(string oldName, string newName, string city = "Basra")
        {
            var requested = new List<string>();

            MockHttp.When(HttpMethod.Get, ItemUrl).Respond(req =>
            {
                var query = req.RequestUri?.Query ?? string.Empty;
                requested.Add(query);

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

            return requested;
        }

        private IRenderedComponent<RevisionCompare<SampleDTO>> RenderCompare(RevisionDTO? newer = null)
        {
            var older = new RevisionDTO { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1) };
            newer ??= new RevisionDTO { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1) };

            var comp = RenderComponent<RevisionCompare<SampleDTO>>(parameters => parameters
                .Add(p => p.Endpoint, Endpoint)
                .Add(p => p.Key, "1")
                .Add(p => p.OldRevision, older)
                .Add(p => p.NewRevision, newer)
                .Add(p => p.ChildContent, NameContent));

            // Two panes fetch in sequence; the default one-second wait is a race, not an assertion.
            comp.WaitForState(() => comp.Instance.BothLoaded, TimeSpan.FromSeconds(10));
            return comp;
        }

        [Fact]
        public void EachPaneRendersItsOwnRevisionThroughTheRealForm()
        {
            // The whole point of the feature: one form body, two revisions, two different renders.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            Assert.Equal("Ali", comp.Instance.OldValue!.Name);
            Assert.Equal("Alice", comp.Instance.NewValue!.Name);
            Assert.Contains("Ali", comp.Markup);
            Assert.Contains("Alice", comp.Markup);
        }

        [Fact]
        public void EachPaneIsItsOwnForm()
        {
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            // Two real ShiftEntityForms side by side, each holding its own snapshot.
            var panes = comp.FindComponents<ShiftEntityForm<SampleDTO>>();

            Assert.Equal(2, panes.Count);
            Assert.Equal("Ali", panes[0].Instance.Value.Name);
            Assert.Equal("Alice", panes[1].Instance.Value.Name);
        }

        [Fact]
        public void PanesDoNotCarryTheViewingARevisionBanner()
        {
            // Both panes are revisions, so the banner is noise — and it is taller in one pane than
            // the other whenever only one shows it, which knocks the fields out of alignment.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            Assert.DoesNotContain("You are viewing a revision", comp.Markup);
        }

        [Fact]
        public void ChangedFieldSummaryIsACountRatherThanAFullList()
        {
            // A banner listing every changed field costs a whole row of the space the two panes
            // are competing for, and reads as noise once a form has more than a few changes.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            Assert.Contains("(1)", comp.Markup);
            Assert.Contains("mud-chip", comp.Markup);
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
        public void ChangedFieldsIgnoresTheFrameworksOwnBookkeeping()
        {
            // LastSaveDate and LastSavedByUserID differ between any two revisions by definition, so
            // listing them says nothing and pushes the fields that did change further down.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            var older = new SampleDTO
            {
                Name = "Ali",
                LastSaveDate = new DateTimeOffset(2023, 12, 16, 14, 23, 0, TimeSpan.Zero),
                LastSavedByUserID = "1",
                CreateDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            };
            var newer = new SampleDTO
            {
                Name = "Alice",
                LastSaveDate = new DateTimeOffset(2026, 7, 19, 6, 5, 0, TimeSpan.Zero),
                LastSavedByUserID = "2",
                CreateDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            };

            var changed = comp.Instance.ComputeChangedFields(older, newer);

            Assert.Equal(new[] { "Name" }, changed);
        }

        [Fact]
        public void NoChangedFieldsWhenSnapshotsAreIdentical()
        {
            MockSnapshots("Ali", "Ali"); // identical

            var comp = RenderCompare();

            Assert.Empty(comp.Instance.ChangedFields);
        }

        [Fact]
        public void CurrentRevisionIsReadLiveWhileTheHistoricalOneCarriesAsOf()
        {
            // Regression: the sentinel on the current revision is stamped with the API server's
            // offset. Testing it against DateTime.MaxValue (converted with the *local* offset) made
            // the live row look historical, so it was re-read through a temporal query - which
            // silently drops includes that reach non-temporal tables.
            var requested = MockSnapshots("Ali", "Alice");

            var current = new RevisionDTO
            {
                ValidFrom = new DateTimeOffset(2026, 7, 19, 6, 5, 0, TimeSpan.FromHours(3)),
                ValidTo = new DateTimeOffset(DateTime.MaxValue.Ticks, TimeSpan.FromHours(3)),
            };

            RenderCompare(current);

            Assert.Equal(2, requested.Count);
            Assert.Contains(requested, q => q.Contains("asOf"));
            Assert.Contains(requested, q => q == string.Empty);
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

        [Fact]
        public void PanesDoNotStealTheKeyboardFromTheCompareDialog()
        {
            // An embedded pane is a full ShiftEntityForm; if it registered as a shortcut component
            // it would sit above the compare and swallow Escape.
            MockSnapshots("Ali", "Alice");

            var comp = RenderCompare();

            foreach (var pane in comp.FindComponents<ShiftEntityForm<SampleDTO>>())
            {
                // Register only succeeds for an Id that is not already in the registry, so this
                // holds regardless of what other tests left behind.
                Assert.True(IShortcutComponent.Register(pane.Instance));
                IShortcutComponent.Remove(pane.Instance.Id);
            }
        }
    }
}
