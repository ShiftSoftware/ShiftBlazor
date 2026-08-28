using Bunit;
using ShiftBlazor.Tests.Shared.DTOs;
using ShiftBlazor.Tests.Viewer.Components.ShiftList;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.UI;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

/// <summary>
/// Filter-panel filters are otherwise only ever declared by hand, which left most lists with no way
/// to look a row up by its key. The panel now supplies one unless the list says otherwise.
/// </summary>
public class ShiftListIdFilterTests : ShiftBlazorTestContext
{
    private static List<FilterModelBase> IdFiltersOf(IRenderedFragment cut) =>
        cut.FindComponent<ShiftList<User>>().Instance.Filters.Values
            .Where(x => x.Field == nameof(ShiftEntityDTOBase.ID))
            .ToList();

    [Fact]
    public void ShouldAddIdFilterWhenFilterPanelIsEnabled()
    {
        var cut = RenderComponent<ShiftListTestIdFilter>();

        var idFilter = Assert.Single(IdFiltersOf(cut));

        Assert.IsType<StringFilterModel>(idFilter);
        // IDs travel as hashed keys, so a substring of one means nothing — only equality does.
        Assert.Equal(ODataOperator.Equal, idFilter.Operator);
        Assert.False(idFilter.IsHidden);
    }

    [Fact]
    public void ShouldNotAddIdFilterWithoutTheFilterPanel()
    {
        var cut = RenderComponent<ShiftListTestIdFilter>(parameters => parameters
            .Add(p => p.EnableFilterPanel, false)
        );

        Assert.Empty(IdFiltersOf(cut));
    }

    [Fact]
    public void ShouldNotAddIdFilterWhenDisabled()
    {
        var cut = RenderComponent<ShiftListTestIdFilter>(parameters => parameters
            .Add(p => p.DisableIdFilter, true)
        );

        Assert.Empty(IdFiltersOf(cut));
    }

    [Fact]
    public void ShouldStandAsideForAFilterTheListDeclaresItself()
    {
        var cut = RenderComponent<ShiftListTestIdFilter>(parameters => parameters
            .Add(p => p.DeclareOwnIdFilter, true)
        );

        // The list's own filter is the one that survives — not two ID boxes in the panel.
        var idFilter = Assert.Single(IdFiltersOf(cut));
        Assert.Equal(ODataOperator.Contains, idFilter.Operator);
    }

    [Fact]
    public void ShouldRenderTheIdFilterInThePanel()
    {
        var cut = RenderComponent<ShiftListTestIdFilter>();

        // Present in the panel, not merely in the Filters dictionary.
        var rendered = cut.FindComponents<StringFilterUI>()
            .Select(x => x.Instance.Filter)
            .Where(x => x.Field == nameof(ShiftEntityDTOBase.ID));

        Assert.Single(rendered);
    }
}
