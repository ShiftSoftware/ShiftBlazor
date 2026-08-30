using Bunit;
using MudBlazor;
using RichardSzalay.MockHttp;
using ShiftBlazor.Tests.Shared.DTOs;
using ShiftBlazor.Tests.Viewer.Components.ShiftList;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

/// <summary>
/// The find box replaces the browser's find-in-page for a virtualized grid: it searches the DTOs,
/// so it matches rows the browser never painted. It is not a filter — nothing reaches the server.
/// </summary>
public class ShiftListFindTests : ShiftBlazorTestContext
{
    private static List<User> SampleUsers() =>
    [
        new() { ID = "aa11", Name = "Ali", Email = "ali@baghdad.example" },
        new() { ID = "bb22", Name = "Sara", Email = "sara@erbil.example" },
        new() { ID = "cc33", Name = "Ali", Email = "ali@erbil.example" },
    ];

    /// <summary>
    /// Replaces the shared random-data mock so row counts can be asserted exactly. A
    /// <paramref name="serverTotal"/> larger than the list stands for a paged endpoint: the page
    /// the grid holds, and a dataset it does not.
    /// </summary>
    private MockedRequest MockUsers(List<User> users, int? serverTotal = null)
    {
        MockHttp.ResetBackendDefinitions();

        return MockHttp
            .When(ODataBaseUrl + "/Users")
            .RespondJson(new ODataDTO<User> { Count = serverTotal ?? users.Count, Value = users });
    }

    private static ShiftList<User> ListOf(IRenderedFragment cut) =>
        cut.FindComponent<ShiftList<User>>().Instance;

    private static int RowCount(IRenderedFragment cut) =>
        cut.FindAll(".mud-table-body .mud-table-row").Count;

    [Fact]
    public void ShouldRenderFindBoxByDefault()
    {
        var cut = RenderComponent<ShiftListTestFind>();

        Assert.NotEmpty(cut.FindAll(".shift-list-find input"));
    }

    [Fact]
    public void ShouldPlaceFindBoxOnItsOwnRowNotInTheToolbar()
    {
        var cut = RenderComponent<ShiftListTestFind>();

        // Inside the toolbar its position depended on how many action buttons the list declared,
        // so it landed somewhere different on every list. Its own row keeps it put.
        Assert.Empty(cut.FindAll(".shift-toolbar-header .shift-list-find"));
        Assert.NotEmpty(cut.FindAll(".shift-list-find-bar .shift-list-find input"));
    }

    [Fact]
    public void ShouldKeepTheBoxDockedWhenTheCounterAppears()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() =>
        {
            // The row is docked to its end, so the counter has to come first. After the box it
            // would shove it away from the edge the moment a search became active — the box would
            // move as you type, which is the problem this row exists to solve.
            var children = cut.Find(".shift-list-find-bar").Children;

            Assert.Equal(2, children.Length);
            Assert.Contains("shift-list-find-count", children[0].ClassName);
            Assert.Contains("shift-list-find", children[1].ClassName);
        });
    }

    [Fact]
    public void ShouldNotRenderFindBoxWhenDisabled()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.DisableFind, true)
        );

        Assert.Empty(cut.FindAll(".shift-list-find"));
    }

    [Fact]
    public void ShouldNarrowRowsToMatches()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));
    }

    [Fact]
    public void ShouldMatchFieldsTheGridDoesNotShow()
    {
        // The ID column is not rendered on this list, but find reads the DTO, not the cells.
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("bb22"));

        cut.WaitForAssertion(() => Assert.Equal(1, RowCount(cut)));
    }

    [Fact]
    public void ShouldRequireEveryTermButNotTheirOrder()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        // "ali" lives in Name and "baghdad" in Email; only one row carries both.
        cut.InvokeAsync(() => ListOf(cut).SetFindText("baghdad ali"));

        cut.WaitForAssertion(() => Assert.Equal(1, RowCount(cut)));
    }

    [Fact]
    public void ShouldRestoreEveryRowWhenCleared()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("sara"));
        cut.WaitForAssertion(() => Assert.Equal(1, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText(null));
        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));
    }

    [Fact]
    public void ShouldReportHowManyOfTheLoadedRowsMatched()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() =>
        {
            Assert.True(ListOf(cut).IsFindActive);
            Assert.Equal(3, ListOf(cut).FindTotalCount);
            Assert.Contains("2", cut.Find(".shift-list-find-count").TextContent);
        });
    }

    [Fact]
    public void ShouldSayNothingMatchedRatherThanLookEmpty()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("nobody-by-this-name"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(0, RowCount(cut));
            // The page is not empty — the find box is hiding all of it, and the grid must say so
            // instead of claiming the list has no items.
            Assert.Contains("nobody-by-this-name", cut.Markup);
        });
    }

    [Fact]
    public void ShouldClearImmediatelyRatherThanOnTheDebounce()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));
        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));

        cut.Find("button.mud-input-clear-button").Click();

        // Asserted synchronously on purpose: MudBlazor runs the clear button through the same
        // debounce as typing, so left alone the rows would not come back for half a second on the
        // commonest way out of a search. WaitForAssertion here would pass either way and prove
        // nothing.
        Assert.Equal(3, RowCount(cut));
        Assert.False(ListOf(cut).IsFindActive);
    }

    [Fact]
    public void ShouldNotSendARequestWhileFinding()
    {
        var request = MockUsers(SampleUsers());
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));
        var requestsBefore = MockHttp.GetMatchCount(request);

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));
        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText(null));
        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        // Find answers from the page already in hand; the pager, sort and filters own the network.
        Assert.Equal(requestsBefore, MockHttp.GetMatchCount(request));
    }

    [Fact]
    public void ShouldPayForItsOwnBarOutOfTheGridHeight()
    {
        MockUsers(SampleUsers(), serverTotal: 1057);
        var cut = RenderComponent<ShiftListTestFindScope>(parameters => parameters
            .Add(p => p.Height, "500px")
        );

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        // The find bar is laid out outside the grid's height box, so its height comes out of it.
        Assert.Equal("calc(500px - var(--shift-list-find-bar-height))", ListOf(cut).EffectiveHeight);

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));
        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));

        // The bar is always there, so the deduction is a constant: nothing the find does can
        // resize the grid underneath the rows the user is reading.
        Assert.Equal("calc(500px - var(--shift-list-find-bar-height))", ListOf(cut).EffectiveHeight);

        cut.InvokeAsync(() => ListOf(cut).SetFindText("nobody-by-this-name"));
        cut.WaitForAssertion(() => Assert.Equal(0, RowCount(cut)));

        Assert.Equal("calc(500px - var(--shift-list-find-bar-height))", ListOf(cut).EffectiveHeight);
    }

    [Fact]
    public void ShouldLeaveTheGridHeightAloneWithoutAFindBar()
    {
        MockUsers(SampleUsers());
        var cut = RenderComponent<ShiftListTestFindScope>(parameters => parameters
            .Add(p => p.Height, "500px")
            .Add(p => p.DisableFind, true)
        );

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        Assert.Equal("500px", ListOf(cut).EffectiveHeight);
    }

    [Fact]
    public void ShouldPointAtTheFilterPanelOnlyWhenThereIsOne()
    {
        MockUsers(SampleUsers(), serverTotal: 1057);
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));
        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        // ShiftListTest1 has no filter panel, so sending the user to one would be a dead end.
        cut.WaitForAssertion(() =>
        {
            var chip = cut.Find(".shift-list-find-scope").TextContent;

            Assert.Contains("1,054", chip);
            Assert.DoesNotContain("filter panel", chip, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ShouldWarnThatOtherPagesWereNotSearched()
    {
        // 3 rows in hand, 1,057 in the table: find answered from 3 and ignored 1,054.
        MockUsers(SampleUsers(), serverTotal: 1057);
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1054, ListOf(cut).FindUnsearchedCount);
            Assert.True(ListOf(cut).IsFindIncomplete);

            // Said once, in the bar the count sits in: the count is only readable next to the
            // scope it was counted over.
            Assert.Contains("1,054", cut.Find(".shift-list-find-scope").TextContent);
        });
    }

    [Fact]
    public void ShouldStaySilentWhenThePageHoldsEverything()
    {
        // Nothing was missed, so a warning here would only teach people to ignore warnings.
        MockUsers(SampleUsers());
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));

        Assert.Equal(0, ListOf(cut).FindUnsearchedCount);
        Assert.False(ListOf(cut).IsFindIncomplete);
        Assert.Empty(cut.FindAll(".shift-list-find-scope"));
    }

    [Fact]
    public void ShouldNotWarnBeforeAnythingIsTyped()
    {
        MockUsers(SampleUsers(), serverTotal: 1057);
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        // The scope only matters once a find has produced an answer to misread.
        Assert.Empty(cut.FindAll(".shift-list-find-scope"));
    }

    [Fact]
    public void ShouldRepeatTheWarningInTheEmptyState()
    {
        MockUsers(SampleUsers(), serverTotal: 1057);
        var cut = RenderComponent<ShiftListTest1>();

        cut.WaitForAssertion(() => Assert.Equal(3, RowCount(cut)));

        cut.InvokeAsync(() => ListOf(cut).SetFindText("nobody-by-this-name"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(0, RowCount(cut));

            // "No row matches" is the reading most likely to be taken as "the record does not
            // exist", so the empty state spells the scope out rather than leaving it to the chip.
            Assert.Contains("1,054", cut.Find(".shift-list-find-unsearched").TextContent);

            // The chip is the constant — it stays in both states.
            Assert.Contains("1,054", cut.Find(".shift-list-find-scope").TextContent);
        });
    }

    [Fact]
    public void ShouldWordTheMatchCountRatherThanGiveItAsNOfM()
    {
        var cut = RenderComponent<ShiftListTestFind>(parameters => parameters
            .Add(p => p.Users, SampleUsers())
        );

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));

        cut.WaitForAssertion(() =>
        {
            // "1 of 100" reads as a complete answer: the denominator is only the page size, and
            // invites the numerator to be taken for the whole truth.
            var count = cut.Find(".shift-list-find-count").TextContent;

            Assert.Contains("2", count);
            Assert.DoesNotContain(" of ", count);
        });
    }

    [Fact]
    public void ShouldKeepThePagerTotalWhileFinding()
    {
        MockUsers(SampleUsers());
        var cut = RenderComponent<ShiftListTest1>();
        var grid = cut.FindComponent<MudDataGrid<User>>();

        cut.WaitForAssertion(() => Assert.Equal(3, grid.Instance.GetFilteredItemsCount()));

        cut.InvokeAsync(() => ListOf(cut).SetFindText("ali"));
        cut.WaitForAssertion(() => Assert.Equal(2, RowCount(cut)));

        // Find hides rows; it must not make the list claim the dataset itself shrank.
        Assert.Equal(3, grid.Instance.GetFilteredItemsCount());
    }
}
