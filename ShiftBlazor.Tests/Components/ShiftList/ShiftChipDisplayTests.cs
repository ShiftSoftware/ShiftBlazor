using MudBlazor;
using ShiftBlazor.Tests.Viewer.Components.ShiftList;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

public class ShiftChipDisplayTests : ShiftBlazorTestContext
{
    private static List<string> MakeItems(int count)
        => Enumerable.Range(1, count).Select(i => $"Item {i}").ToList();

    [Fact]
    public void ShouldRenderNothingWhenEmpty()
    {
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, [])
        );

        Assert.Empty(comp.FindAll(".shift-chip-display"));
    }

    [Fact]
    public void ShouldRenderAllChipsWhenAtOrBelowMaxVisible()
    {
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, MakeItems(3))
            .Add(p => p.MaxVisible, 3)
        );

        Assert.Equal(3, comp.FindAll(".shift-chip-display__item").Count);
        Assert.Empty(comp.FindAll(".shift-chip-display__overflow"));
    }

    [Fact]
    public void ShouldCollapseOverflowIntoPlusNChip()
    {
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, MakeItems(5))
            .Add(p => p.MaxVisible, 2)
        );

        Assert.Equal(2, comp.FindAll(".shift-chip-display__item").Count);

        var overflow = comp.Find(".shift-chip-display__overflow");
        Assert.Contains("+3", overflow.TextContent);
    }

    [Fact]
    public void ShouldOpenDialogWithAllChipsOnPlusNClick()
    {
        var comp = RenderComponent<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftChipDisplayTest>(chipParams => chipParams
                .Add(p => p.Items, MakeItems(5))
                .Add(p => p.MaxVisible, 2)
            )
        );

        comp.Find(".shift-chip-display__more-chip").Click();

        // The popup lists the FULL set, not just the hidden remainder.
        var dialogChips = comp.FindComponents<ShiftChipDialog>();
        Assert.Single(dialogChips);
        Assert.Equal(5, dialogChips[0].FindComponents<MudChip<string>>().Count);
    }

    [Fact]
    public void ShouldShowCountOnlyChipWhenMaxVisibleIsZero()
    {
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, MakeItems(5))
            .Add(p => p.MaxVisible, 0)
        );

        // Count-only mode: no inline chips, one chip with the TOTAL (no "+" — it isn't a remainder).
        Assert.Empty(comp.FindAll(".shift-chip-display__item"));
        var overflow = comp.Find(".shift-chip-display__overflow").TextContent.Trim();
        Assert.Equal("5", overflow);
    }

    [Fact]
    public void ShouldClampNegativeMaxVisibleToCountOnlyMode()
    {
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, MakeItems(5))
            .Add(p => p.MaxVisible, -2)
        );

        // Negative values clamp to 0 (count-only mode) instead of producing a bogus "+7" remainder.
        Assert.Empty(comp.FindAll(".shift-chip-display__item"));
        Assert.Equal("5", comp.Find(".shift-chip-display__overflow").TextContent.Trim());
    }

    [Fact]
    public void ShouldNotUseJsInterop()
    {
        // The deterministic collapse must not measure the DOM: no JS interop, no ResizeObserver.
        var comp = RenderComponent<ShiftChipDisplayTest>(parameters => parameters
            .Add(p => p.Items, MakeItems(5))
            .Add(p => p.MaxVisible, 2)
        );

        Assert.Equal(2, comp.FindAll(".shift-chip-display__item").Count);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier.Contains("shiftChipOverflow"));
    }
}
