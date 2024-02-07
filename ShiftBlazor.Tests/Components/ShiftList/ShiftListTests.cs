using Bunit;
using Bunit.Rendering;
using MudBlazor;
using ShiftBlazor.Tests.Viewer.Components.ShiftEntityForm;
using ShiftBlazor.Tests.Viewer.Components.ShiftList;
using ShiftBlazor.Tests.Shared.DTOs;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

public class ShiftListTests : ShiftBlazorTestContext
{
    [Fact]
    public void ShouldRenderComponent()
    {
        RenderComponent<ShiftListTestLocalData>();
    }

    [Fact]
    public void ShouldThrowExceptionIfActionAndValueAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            RenderComponent<ShiftList<SampleDTO>>();
        });
    }

    [Fact]
    public void ShouldShowErrorSnackbarWhenBadUrl()
    {
        var cut = RenderComponent<ShiftListTestHttpError>();

        var alert = cut.FindComponent<MudAlert>();

        Assert.Equal(Severity.Error, alert.Instance.Severity);
    }

    [Fact]
    public void ShouldRenderRowsPerPageCorrectly()
    {
        var cut = RenderComponent<ShiftListTestLocalData>();

        var grid = cut.FindComponent<MudDataGrid<User>>();

        cut.WaitForAssertion(() =>
        {
            var rows = grid.FindAll(".mud-table-body .mud-table-row");
            Assert.Equal(grid.Instance.RowsPerPage, rows.Count());
        });
    }

    [Fact]
    public void ShouldRenderTitleCorrectly()
    {
        //get a random string
        var title = Guid.NewGuid().ToString();

        var cut = RenderComponent<ShiftListTestLocalData>(parameters => parameters
            .Add(p => p.Title, title)
        );

        Assert.Contains(title, cut.Markup);
        //TODO: check page title?
    }

    [Fact]
    public void ShouldRenderPagingByDefault()
    {
        var cut = RenderComponent<ShiftListTest1>();
        var grid = cut.FindComponent<ShiftList<User>>();
        Assert.True(cut.HasComponent<MudDataGridPager<User>>());
        Assert.False(grid.Instance.DisablePagination);
    }

    [Fact]
    public void ShouldBeAbleToSetDataGridPageSize()
    {
        var pageSize = 22;

        var cut = RenderComponent<ShiftListTest2>(parameters => parameters
            .Add(p => p.PageSize, pageSize)
        );

        var grid = cut.FindComponent<MudDataGrid<User>>();
        Assert.Equal(pageSize, grid.Instance.RowsPerPage);
    }

    [Fact]
    public void ShouldHideIDColumnByDefault()
    {
        var comp = RenderComponent<ShiftListTest1>();

        var comp2 = RenderComponent<ShiftListTest2>();

        var cols1 = comp.FindComponent<MudDataGrid<User>>().Instance.RenderedColumns;
        var cols2 = comp2.FindComponent<MudDataGrid<User>>().Instance.RenderedColumns;

        Assert.Null(cols1.FirstOrDefault(x => x.PropertyName == nameof(User.ID)));
        Assert.NotNull(cols2.FirstOrDefault(x => x.PropertyName == nameof(User.ID)));
    }

    [Fact]
    public void ShouldNotRenderActionColumn()
    {
        // Should not render Actions column when ComponentType is not set
        var comp = RenderComponent<ShiftListTest1>();
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        var cols = grid.RenderedColumns.Where(x => x.Title == "Actions");
        Assert.Empty(cols);
    }

    [Fact]
    public void ShouldRenderOrHideActionColumn2()
    {
        // Should render Actions column when ComponentType is 
        var comp = RenderComponent<ShiftListTest2>();
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        var cols = grid.RenderedColumns.Where(x => x.Title == "Actions");
        Assert.Single(cols);
    }

    [Fact]
    public void ShouldRenderOrHideActionColumn3()
    {
        // Should not render Actions column when DisableActionColumn is true
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisableActionColumn, true)
        );
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;
        var cols = grid.RenderedColumns.Where(x => x.Title == "Actions");
        Assert.Empty(cols);
    }

    [Fact]
    public void ShouldNotRenderExportButton()
    {
        var comp = RenderComponent<ShiftListTest1>();

        var tooltip = comp.FindComponents<MudTooltip>().FirstOrDefault(x => x.Instance.Text.Contains("Export"));
        Assert.Null(tooltip);
    }

    [Fact]
    public void ShouldRenderExportButton()
    {
        var comp = RenderComponent<ShiftListTestExport>();

        var tooltip = comp.FindComponents<MudTooltip>().FirstOrDefault(x => x.Instance.Text.Contains("Export"));
        Assert.NotNull(tooltip);
    }

    [Fact]
    public void ShouldEnableVirtualizationAndDisablePaging()
    {
        var comp = RenderComponent<ShiftListTestVirtualization>(parameters => parameters.Add(p => p.EnableVirtualization, true));

        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        Assert.True(grid.Virtualize);
        Assert.False(comp.HasComponent<MudDataGridPager<User>>(), "Found MudDataGridPager");
    }

    [Fact]
    public void ShouldNotRenderAddButton()
    {
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisableAdd, true)
        );

        var instance = comp.FindComponent<ShiftList<User>>().Instance;

        Assert.False(instance.RenderAddButton);

        var tooltips = comp.FindComponents<MudTooltip>();

        Assert.Null(tooltips.FirstOrDefault(x => x.Instance.Text.Equals("Add")));
    }

    [Fact]
    public void ShouldDisableDataGridPaging()
    {
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisablePagination, true)
        );

        Assert.False(comp.HasComponent<MudDataGridPager<User>>(), "Found MudDataGridPager");
    }

    [Fact]
    public void ShouldDisableDataGridSorting()
    {
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisableSorting, true)
        );
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        Assert.Equal(SortMode.None, grid.SortMode);
    }

    [Fact]
    public void ShouldDisableDataGridMultiSorting()
    {
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisableMultiSorting, true)
        );
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        Assert.Equal(SortMode.Single, grid.SortMode);
    }

    [Fact]
    public void ShouldDisableDataGridFilters()
    {
        var comp = RenderComponent<ShiftListTestDisableFeatures>(parameters => parameters
            .Add(p => p.DisableFilters, true)
        );
        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        Assert.False(grid.Filterable);
    }

    //[Fact]
    //public void ShouldDisableDataGridSelection()
    //{
    //    var comp = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
    //        .Add(p => p.Action, "/Product")
    //        .Add(p => p.ComponentType, typeof(DummyComponent))
    //        .Add(p => p.DisablePagination, DisablePaging)
    //    );
    //    var grid = comp.FindComponent<SfGrid<SampleDTO>>().Instance;

    //    Assert.False(grid.AllowSelection);
    //}

    //[Fact]
    //public void ShouldAddColumnsToDataGrid()
    //{
    //    var headerText = "This is a header";
    //    var comp = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
    //        .Add(p => p.EntitySet, "/Product")
    //        .Add(p => p.ComponentType, typeof(DummyComponent))
    //        .Add<GridColumn>(p => p.ColumnTemplate, _params => _params.Add(x => x.HeaderText, headerText))
    //        .Add(p => p.DisablePagination, DisablePaging)
    //    );

    //    var grid = comp.FindComponent<SfGrid<SampleDTO>>().Instance;
    //    var customColumn = grid.Columns.FirstOrDefault(x => x.HeaderText.Equals(headerText));
    //    Assert.NotNull(customColumn);
    //}

    [Fact]
    public void ShouldReplaceActionsColumnContent()
    {
        var text = Guid.NewGuid().ToString();
        var comp = RenderComponent<ShiftListTestCustomColumns>(parameters => parameters
            .Add(p => p.ActionsTemplate, $"<h1>{text}</h1>")
        );

        var grid = comp.FindComponent<MudDataGrid<User>>();

        var row = comp.Find(".mud-table-cell[data-label='Actions']");
        row.FirstChild?.ToMarkup().Contains($"{text}");

    }

    [Fact]
    public void ShouldAddElementsToToolbar()
    {
        var text = Guid.NewGuid();

        var comp = RenderComponent<ShiftListTestCustomToolbar>(parameters => parameters
            .Add(p => p.ToolbarStartTemplate, $"<span>{text}</span>")
        );

        var toolbar = comp.Find(".shift-toolbar-header");

        Assert.Contains($"<span>{text}</span>", toolbar.ToMarkup());
    }

    //[Fact]
    //public void ShouldAddWhereToFilter()
    //{
    //    Expression<Func<User, bool>> WhereFilter = (x) => x.Name.Contains("Be");
    //    var comp = RenderComponent<ShiftListTestFilteredResult>(parameters => parameters
    //        .Add(p => p.Where, WhereFilter)
    //    );
    //    var grid = comp.FindComponent<ShiftList<User>>().Instance;
    //    // $filter=contains(Name,'Be')
    //    Assert.Equal(WhereFilter, grid.Where);
    //    Assert.Contains("$filter=contains(Name,'Be')", grid.CurrentUri?.Query);
    //}

    [Fact]
    public void ShouldSetDataGridHeight()
    {
        var height = "300px";
        var comp = RenderComponent<ShiftListTestVirtualization>(parameters => parameters
            .Add(p => p.EnableVirtualization, true)
            .Add(p => p.Height, height)
        );

        var grid = comp.FindComponent<MudDataGrid<User>>().Instance;

        Assert.Equal(height, grid.Height);
    }

    [Fact]
    public void ShouldEmbededInsideForm()
    {
        RenderTree.Add<ShiftFormBasic<SampleDTO>>();

        var comp = RenderComponent<ShiftEntityFormTestWithList>();

        var list = comp.FindComponent<ShiftList<User>>();

        Assert.True(list.Instance.IsEmbed);
    }

    //[Fact]
    //public void ShouldCreateCorrectDeleteQuery()
    //{
    //    var comp = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
    //        .Add(p => p.Action, "/Product")
    //        .Add(p => p.DisablePagination, DisablePaging)
    //    );

    //    var shiftList = comp.Instance;

    //    // Show only items that are not deleted
    //    var active = shiftList.CreateDeleteQuery(ShiftList<SampleDTO>.DeleteFilter.Active);
    //    Assert.Single(active.Queries.Where);
    //    var activeWhere = active.Queries.Where.First();
    //    Assert.Equal(nameof(ShiftEntityDTOBase.IsDeleted), activeWhere.Field);
    //    Assert.Equal("equal", activeWhere.Operator);
    //    Assert.False((bool)activeWhere.value);
    //    Assert.False(activeWhere.IsComplex);
    //    Assert.Null(activeWhere.predicates);
    //    Assert.Null(activeWhere.Condition);

    //    // Show only items that are deleted
    //    var deleted = shiftList.CreateDeleteQuery(ShiftList<SampleDTO>.DeleteFilter.Deleted);
    //    Assert.Single(deleted.Queries.Where);
    //    var deletedWhere = deleted.Queries.Where.First();
    //    Assert.Equal(nameof(ShiftEntityDTOBase.IsDeleted), deletedWhere.Field);
    //    Assert.Equal("equal", deletedWhere.Operator);
    //    Assert.True((bool)deletedWhere.value);
    //    Assert.False(activeWhere.IsComplex);
    //    Assert.Null(deletedWhere.predicates);
    //    Assert.Null(deletedWhere.Condition);

    //    // Show all items
    //    var all = shiftList.CreateDeleteQuery(ShiftList<SampleDTO>.DeleteFilter.All);
    //    Assert.Single(all.Queries.Where);
    //    var allWhereGrouping = all.Queries.Where.First();
    //    Assert.Null(allWhereGrouping.Field);
    //    Assert.Null(allWhereGrouping.Operator);
    //    Assert.Null(allWhereGrouping.value);
    //    Assert.True(allWhereGrouping.IsComplex);
    //    Assert.Equal("and", allWhereGrouping.Condition);
    //    Assert.Equal(2, allWhereGrouping.predicates.Count);

    //    // we have an extra always true filter to force syncfusion to create a correct grouped sql (1 or 1)
    //    var allExtraPredicate = allWhereGrouping.predicates.First();
    //    Assert.Equal("1", allExtraPredicate.Field);
    //    Assert.Equal("equal", allExtraPredicate.Operator);
    //    Assert.Equal(1, (int)allExtraPredicate.value);

    //    var allWheres = allWhereGrouping.predicates.ElementAt(1);
    //    Assert.Equal("or", allWheres.Condition);
    //    Assert.Null(allWheres.value);
    //    Assert.Equal(2, allWheres.predicates.Count);

    //    var allActive = allWheres.predicates.First();
    //    var allDeleted = allWheres.predicates.ElementAt(1);
    //    Assert.Equal(nameof(ShiftEntityDTOBase.IsDeleted), allActive.Field);
    //    Assert.Equal("equal", allActive.Operator);
    //    Assert.False((bool)allActive.value);
    //    Assert.Equal(nameof(ShiftEntityDTOBase.IsDeleted), allDeleted.Field);
    //    Assert.Equal("equal", allDeleted.Operator);
    //    Assert.True((bool)allDeleted.value);
    //}

    //// when filtering multiple times, it should add all of them to the query, only the current selected one
    //[Fact]
    //public void ShouldNotStackDeleteFilters()
    //{
    //    var comp = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
    //        .Add(p => p.Action, "/Product")
    //        .Add(p => p.DisablePagination, DisablePaging)
    //    );

    //    var shiftList = comp.Instance;

    //    _ = shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.Active);
    //    _ = shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.Deleted);
    //    _ = shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.Deleted);
    //    _ = shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.All);
    //    _ = shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.Active);

    //    Assert.Single(shiftList.GridQuery!.Queries.Where);
    //}

    //[Fact]
    //public async Task ShouldCombineDeleteFiltersButKeepOriginal()
    //{
    //    var originalQuery = new Query().Where(nameof(SampleDTO.Name), "equal", "Sample 1");

    //    var comp = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
    //        .Add(p => p.Action, "/Product")
    //        .Add(p => p.DisablePagination, DisablePaging)
    //        .Add(p => p.Query, originalQuery)
    //    );

    //    var shiftList = comp.Instance;

    //    await shiftList.FilterDeleted(ShiftList<SampleDTO>.DeleteFilter.Active);

    //    Assert.Single(shiftList.Query!.Queries.Where);
    //    var originalWhere = shiftList.Query!.Queries.Where.First();
    //    Assert.Equal(nameof(SampleDTO.Name), originalWhere.Field);
    //    Assert.Equal("equal", originalWhere.Operator);
    //    Assert.Equal("Sample 1", (string)originalWhere.value);

    //    Assert.Equal(2, shiftList.GridQuery!.Queries.Where.Count);
    //}
}