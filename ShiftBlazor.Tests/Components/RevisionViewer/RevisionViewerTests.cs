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
        public async Task CompareHandlerEmitsOlderRevisionFirst()
        {
            CompareRevisions? emitted = null;
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl)
                .Add(p => p.OnCompareRequested, EventCallback.Factory.Create<CompareRevisions>(this, r => emitted = r)));

            var newer = new RevisionDTO { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1), ID = "2" };
            var older = new RevisionDTO { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1), ID = "1" };

            // Add newest first to prove ordering, not insertion order, decides Old/New.
            comp.Instance.SelectedRevisions.Add(newer);
            comp.Instance.SelectedRevisions.Add(older);

            await comp.InvokeAsync(() => comp.Instance.CompareHandler());

            Assert.NotNull(emitted);
            Assert.Equal(older.ValidFrom, emitted!.Old.ValidFrom);
            Assert.Equal(newer.ValidFrom, emitted.New.ValidFrom);
        }

        [Fact]
        public async Task CompareHandlerEmitsNothingUnlessTwoSelected()
        {
            CompareRevisions? emitted = null;
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters
                .Add(p => p.EntitySet, RevisionsEntitySet)
                .Add(p => p.ItemUrl, ItemUrl)
                .Add(p => p.OnCompareRequested, EventCallback.Factory.Create<CompareRevisions>(this, r => emitted = r)));

            await comp.InvokeAsync(() => comp.Instance.CompareHandler());
            Assert.Null(emitted);

            comp.Instance.SelectedRevisions.Add(new() { ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2021, 1, 1) });
            await comp.InvokeAsync(() => comp.Instance.CompareHandler());
            Assert.Null(emitted);

            comp.Instance.SelectedRevisions.Add(new() { ValidFrom = new DateTime(2021, 1, 1), ValidTo = new DateTime(2022, 1, 1) });
            await comp.InvokeAsync(() => comp.Instance.CompareHandler());
            Assert.NotNull(emitted);
        }
    }
}
