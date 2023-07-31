using Bunit.Rendering;
using MudBlazor;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.Grids;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

public class ShiftListTests : ShiftBlazorTestContext
{
    public static bool DisablePaging = true;

    [Fact]
    public void ShouldShowErrorIfActionAndValueAreNull()
    {
        var cut = RenderComponent<ShiftList<Sample>>(paramaters =>
            paramaters.Add(p => p.DisablePagination, DisablePaging));

        var alertComp = cut.FindComponent<MudAlert>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(alertComp);
            Assert.Contains("Both Action and Values parameters cannot be null", alertComp.Markup);
        });
    }

    //[Fact]
    //public void ShouldShowErrorSnackbarWhenBadUrl()
    //{
    //    var cut = RenderComponent<ShiftList<ShiftEntityDTO>>(parameters => parameters.Add(p => p.Action, "/404-url"));

    //    cut.WaitForElement(".disable-grid");
    //}

    //[Fact]
    //public void ShouldRenderRowsPerPageCorrectly()
    //{
    //    var cut = Render(@<ShiftList T="Sample" Action="@("/Product")"></ShiftList>);

    //    var grid = cut.FindComponent<SfGrid<Sample>>();

    //    var rows = grid.FindAll(".e-content .e-table > tbody > .e-row");

    //    cut.WaitForAssertion(() =>
    //    {
    //        Assert.Equal(grid.Instance.PageSettings.PageSize, grid.FindAll(".e-row").Count);
    //    });

    //}

    /// <summary>
    ///     Title
    /// </summary>
    [Fact]
    public void ShouldRenderTitleCorrectly()
    {
        //get a random string
        var title = Guid.NewGuid().ToString();

        var cut = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.Title, title)
            .Add(p => p.DisablePagination, DisablePaging)
        );

        Assert.Contains(title, cut.Markup);
        //TODO: check page title?
    }

    /// <summary>
    ///     Title
    /// </summary>
    [Fact]
    public void ShouldRenderPagingByDefault()
    {
        var cut = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
        );

        var grid = cut.FindComponent<SfGrid<Sample>>();

        Assert.True(grid.Instance.AllowPaging);
    }

    /// <summary>
    ///     page size
    /// </summary>
    [Fact]
    public void ShouldBeAbleToSetDataGridPageSize()
    {
        var pageSize = 22;

        var cut = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.PageSize, pageSize)
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = cut.FindComponent<SfGrid<Sample>>();
        Assert.Equal(pageSize, grid.Instance.PageSettings.PageSize);
    }

    /// <summary>
    ///     ExcludedHeaders
    /// </summary>
    [Fact]
    public void ShouldHideExcludedColumns2()
    {
        List<string> excludedColumns = new()
        {
            nameof(Sample.LastName),
            nameof(Sample.City)
        };

        var cut = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ExcludedColumns, excludedColumns)
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = cut.FindComponent<SfGrid<Sample>>();

        var columns = grid.Instance.Columns;
        var properties = typeof(Sample).GetProperties();

        // we have an extra Actions column in columns variable but also missing Revision column since we hide it by default
        Assert.Equal(properties.Count() - excludedColumns.Count, columns.Count());
        Assert.All(columns, x => excludedColumns.Contains(x.Field).Equals(false));
    }

    [Fact]
    public void ShouldHideColumnsByDefault()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = comp.FindComponent<SfGrid<Sample>>();

        var revisionsColumns = grid.Instance.Columns.FirstOrDefault(x => x.Field.Equals(nameof(Sample.Revisions)));
        var idColumns = grid.Instance.Columns.Where(x => x.Field.Equals(nameof(Sample.ID)));
        Assert.Null(revisionsColumns);
        Assert.Single(idColumns);
        Assert.False(idColumns.First().Visible);
    }

    [Fact]
    public void ShouldRenderOrHideActionColumn1()
    {
        // Should not render Actions column when ComponentType is not set
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        var cols = grid.Columns.Where(x => x.HeaderText == "Actions");
        Assert.Empty(cols);
    }

    [Fact]
    public void ShouldRenderOrHideActionColumn2()
    {
        // Should render Actions column when ComponentType is 
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        var cols = grid.Columns.Where(x => x.HeaderText == "Actions");
        Assert.Single(cols);
    }

    [Fact]
    public void ShouldRenderOrHideActionColumn3()
    {
        List<string> excludedColumns = new()
        {
            "Actions"
        };

        // Should not render Actions column when it is excluded in ExcludedColumns
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ExcludedColumns, excludedColumns)
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;
        var cols = grid.Columns.Where(x => x.HeaderText == "Actions");
        Assert.Empty(cols);
    }

    [Fact]
    public void ShouldNotRenderDownloadButton()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.EnableCsvExcelExport, false)
            .Add(p => p.EnablePdfExport, false)
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var menu = comp.FindComponents<MudMenu>().FirstOrDefault(x => x.Instance.Class == "download-options");
        Assert.Null(menu);
    }

    [Fact]
    public void ShouldOnlyRenderCSVnExcelButton()
    {
        var comp = RenderComponent<IncludeMudProviders>(_params => _params.AddChildContent<ShiftList<Sample>>(
            parameters => parameters
                .Add(p => p.Action, "/Product")
                .Add(p => p.ComponentType, typeof(DummyComponent))
                .Add(p => p.EnableCsvExcelExport, true)
                .Add(p => p.EnablePdfExport, false)
                .Add(p => p.DisablePagination, DisablePaging)
        ));

        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowPdfExport);
        Assert.True(grid.AllowExcelExport);

        comp.FindAll(".mud-menu.download-options button.mud-button-root")[0].Click();

        comp.WaitForAssertion(() =>
        {
            Assert.Equal(2, comp.FindAll("div.mud-list-item").Count);

            Assert.DoesNotContain("PDF Export", comp.Markup);
            Assert.Contains("CSV Export", comp.Markup);
            Assert.Contains("Excel Export", comp.Markup);
        });
    }

    [Fact]
    public void ShouldOnlyRenderPDFButton()
    {
        var comp = RenderComponent<IncludeMudProviders>(_params => _params.AddChildContent<ShiftList<Sample>>(
            parameters => parameters
                .Add(p => p.Action, "/Product")
                .Add(p => p.ComponentType, typeof(DummyComponent))
                .Add(p => p.EnableCsvExcelExport, false)
                .Add(p => p.EnablePdfExport, true)
                .Add(p => p.DisablePagination, DisablePaging)
        ));

        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowExcelExport);
        Assert.True(grid.AllowPdfExport);

        comp.FindAll(".mud-menu.download-options button.mud-button-root")[0].Click();

        comp.WaitForAssertion(() =>
        {
            Assert.Equal(1, comp.FindAll("div.mud-list-item").Count);
            Assert.Contains("PDF Export", comp.Markup);
            Assert.DoesNotContain("CSV Export", comp.Markup);
            Assert.DoesNotContain("Excel Export", comp.Markup);
        });
    }

    [Fact]
    public void ShouldRenderPrintButton()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.EnablePrint, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var tooltips = comp.FindComponents<MudTooltip>();

        var print = tooltips.First(x => x.Instance.Text.Equals("Print"));
        print.Find("button");
    }

    [Fact]
    public void ShouldEnableVirtualizationAndDisablePaging()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableSelection, false)
            .Add(p => p.EnableVirtualization, true)
        );

        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.True(grid.EnableVirtualization);
        Assert.True(grid.EnableVirtualMaskRow);
        Assert.False(grid.AllowPaging);
        // HeaderTemplate should have a value (an empty fragment) to replace the checkbox
        Assert.NotNull(grid.Columns.ElementAt(0).HeaderTemplate);
    }

    [Fact]
    public void ShouldNotRenderAddButton()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableAdd, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var instance = comp.Instance;

        Assert.False(instance.RenderAddButton);

        var tooltips = comp.FindComponents<MudTooltip>();

        Assert.Null(tooltips.FirstOrDefault(x => x.Instance.Text.Equals("Add")));
    }

    [Fact]
    public void ShouldDisableDataGridPaging()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, true)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowPaging);
    }

    [Fact]
    public void ShouldDisableDataGridSorting()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableSorting, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowSorting);
    }

    [Fact]
    public void ShouldDisableDataGridMultiSorting()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableMultiSorting, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowMultiSorting);
    }

    [Fact]
    public void ShouldDisableDataGridFilters()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableFilters, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowFiltering);
    }

    [Fact]
    public void ShouldDisableDataGridSelection()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisableSelection, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.False(grid.AllowSelection);
    }

    [Fact]
    public void ShouldAddColumnsToDataGrid()
    {
        var headerText = "This is a header";
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add<GridColumn>(p => p.ColumnTemplate, _params => _params.Add(x => x.HeaderText, headerText))
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;
        var customColumn = grid.Columns.FirstOrDefault(x => x.HeaderText.Equals(headerText));
        Assert.NotNull(customColumn);
    }

    [Fact]
    public async Task ShouldReplaceActionsColumnContent()
    {
        var text = "Hello World!";
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.ActionsTemplate, item => $"<h1>{text}</h1>")
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = comp.FindComponent<SfGrid<Sample>>();

        await Task.Delay(300);

        var row = grid.Find(".e-table .e-row .e-rowcell[aria-label~='Actions']");
        row.FirstChild?.MarkupMatches($"<div><h1>{text}</h1></div>");

    }

    [Fact]
    public void ShouldAddElementsToToolbar()
    {
        var text = "click me!";

        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.ToolbarStartTemplate, $"<button>{text}</button>")
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var toolbar = comp.Find(".shift-toolbar-header");

        Assert.Contains($"<button>{text}</button>", toolbar.ToMarkup());
    }

    [Fact]
    public void ShouldAddQueryToDataGridQuery()
    {
        var query = new Query().Expand(new List<string> { "Customer" });
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.Query, query)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.Equal(query, grid.Query);
    }

    [Fact]
    public void ShouldEnableAutoFitOnActionColumn()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;
        var column = grid.Columns.First(x => x.HeaderText.Equals("Actions"));

        Assert.True(column.AutoFit);
    }

    [Fact]
    public void ShouldSetDataGridHeight()
    {
        var height = "600";
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.GridHeight, height)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        Assert.Equal(height, grid.Height);
    }

    [Fact]
    public void ShouldAutoGenerateColumns()
    {
        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DisablePagination, DisablePaging)
        );
        var instance = comp.Instance;
        var grid = comp.FindComponent<SfGrid<Sample>>().Instance;

        var props = typeof(Sample).GetProperties().Where(x => !instance.DefaultExcludedColumns.Contains(x.Name));
        var fields = grid.Columns.Select(x => x.Field);
        var results = new List<bool>();

        Assert.All(props, x => Assert.Contains(x.Name, fields));
    }

    [Fact]
    public void ShouldEmbededInsideForm()
    {
        RenderTree.Add<ShiftFormBasic<Sample>>();

        var comp = RenderComponent<ShiftList<Sample>>(parameters => parameters
            .Add(p => p.Action, "/Product")
            .Add(p => p.ComponentType, typeof(DummyComponent))
            .Add(p => p.DisablePagination, DisablePaging)
        );

        var toolbar = comp.FindComponent<MudToolBar>();

        Assert.Contains("shift-toolbar-header", toolbar.Instance.Class);

        var tooltips = toolbar.FindComponents<MudTooltip>().Where(x => !x.Instance.Text.Equals("Add"));
        var divider = toolbar.FindComponents<MudDivider>();

        Assert.Empty(tooltips);
        Assert.Empty(divider);
    }
}