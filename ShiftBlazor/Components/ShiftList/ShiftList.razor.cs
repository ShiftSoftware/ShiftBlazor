using Microsoft.AspNetCore.Components;
using MudBlazor;
using Syncfusion.Blazor.Grids;
using System.Reflection;
using Syncfusion.Blazor.Data;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftBlazor.Enums;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Events.CustomEventArgs;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftList<T> : EventComponentBase, IDisposable
        where T : ShiftEntityDTOBase, new()
    {
        [Inject] private MessageService MsgService { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] IStringLocalizer<Resources.Components.ShiftList> Loc { get; set; } = default!;
        [Inject] private TypeAuth.Blazor.Services.TypeAuthService TypeAuthService { get; set; } = default!;

        [CascadingParameter]
        protected MudDialogInstance? MudDialog { get; set; }

        /// <summary>
        ///     To check whether this list is currently embeded inside a form component.
        /// </summary>
        [CascadingParameter(Name = "FormChild")]
        public bool? IsEmbedded { get; set; }

        /// <summary>
        ///     The current fetched items, this will be fetched from the OData API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public List<T>? Values { get; set; }

        /// <summary>
        ///     An event triggered when the state of Values has changed.
        /// </summary>
        [Parameter]
        public EventCallback<T> ValuesChanged { get; set; }

        /// <summary>
        ///     The URL endpoint that processes the CRUD operations.
        /// </summary>
        [Parameter]
        public string? Action { get; set; }

        /// <summary>
        ///     The title to render on the form header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        ///     The number of items to be displayed per page.
        /// </summary>
        [Parameter]
        public int? PageSize { get; set; }

        /// <summary>
        ///     A list of columns names to hide them in the UI.
        /// </summary>
        [Parameter]
        public List<string> ExcludedColumns { get; set; } = new();

        /// <summary>
        ///     Enable CSV And Excel format Download button.
        /// </summary>
        [Parameter]
        public bool EnableCsvExcelExport { get; set; }

        /// <summary>
        ///     Enable PDF format Download button.
        /// </summary>
        [Parameter]
        public bool EnablePdfExport { get; set; }
        
        /// <summary>
        ///     Enable Print button.
        /// </summary>
        [Parameter]
        public bool EnablePrint { get; set; }

        /// <summary>
        ///     Enable Virtualization and disable Paging.
        /// </summary>
        [Parameter]
        public bool EnableVirtualization { get; set; }
        
        /// <summary>
        ///     Disable the add item button to open a form.
        /// </summary>
        [Parameter]
        public bool DisableAdd { get; set; }
        
        /// <summary>
        ///     Disable paging.
        /// </summary>
        [Parameter]
        public bool DisablePagination { get; set; }
        
        /// <summary>
        ///     Disable sorting.
        /// </summary>
        [Parameter]
        public bool DisableSorting { get; set; }
        
        /// <summary>
        ///     Disable multisorting.
        /// </summary>
        [Parameter]
        public bool DisableMultiSorting { get; set; }
        
        /// <summary>
        ///     Disable filtering.
        /// </summary>
        [Parameter]
        public bool DisableFilters { get; set; }
        
        /// <summary>
        ///     Disable select
        /// </summary>
        [Parameter]
        public bool DisableSelection { get; set; }

        /// <summary>
        ///     If true, the toolbar in the header will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableHeaderToolbar { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        ///     An element used to insert GridColumn elements before the Action column.
        /// </summary>
        [Parameter]
        public RenderFragment? ColumnTemplate { get; set; }
        
        /// <summary>
        ///     Used to override any element in the Action column.
        /// </summary>
        [Parameter]
        public RenderFragment<T>? ActionsTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarStartTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarEndTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the controls section of the header toolbar.
        ///     This section is only visible when the form is opened in a dialog.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarControlsTemplate { get; set; }

        /// <summary>
        ///     To pass Syncfusion's OData query data.
        /// </summary>
        [Parameter]
        public Query Query { get; set; } = new();
        
        /// <summary>
        ///     To set the list's fixed height.
        /// </summary>
        [Parameter]
        public string GridHeight { get; set; } = string.Empty;

        /// <summary>
        ///     To pass additional parameters to the ShiftFormContainer componenet.
        /// </summary>
        [Parameter]
        public Dictionary<string, string>? AddDialogParameters { get; set; }
        
        /// <summary>
        ///     To specify whether to generate the Syncfusion columns automatically or not.
        /// </summary>
        [Parameter]
        public bool AutoGenerateColumns { get; set; } = true;

        /// <summary>
        /// The icon displayed before the Form Title, in a string in SVG format.
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = @Icons.Material.Filled.List;

        /// <summary>
        ///     The type of the component to open when clicking on Add or the Action button.
        ///     If empty, Add and Action button column will be hidden.
        /// </summary>
        [Parameter]
        public Type? ComponentType { get; set; }

        [Parameter]
        public EventCallback<RecordClickEventArgs<T>> OnRowClick { get; set; }
        [Parameter]
        public EventCallback<object?> OnFormClosed { get; set; }

        [Parameter]
        public bool ShowIDColumn { get; set; } = false;

        [Parameter]
        public bool DisableColumnChooser { get; set; }

        [Parameter]
        public Dictionary<string, string>? HeaderTexts { get; set; }
        [Parameter]
        public bool MultiLineCells { get; set; }

        [Parameter]
        public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

        public SfGrid<T>? Grid;
        internal List<ListColumn> GeneratedColumns = new();
        internal readonly List<string> DefaultExcludedColumns = new() { nameof(ShiftEntityDTOBase.ID), "Revisions" };
        internal int[] PageSizes = new int[] { 5, 10, 50, 100, 250, 500 };

        internal bool RenderAddButton => !(DisableAdd || ComponentType == null || (TypeAuthAction != null && !TypeAuthService.Can(TypeAuthAction, TypeAuth.Core.Access.Write)));
        internal bool ActionUrlBroken = false;
        internal bool IsReady = false;

        internal string GridId;
        internal FilterSettings FilterSettingMenu = new FilterSettings { Type = Syncfusion.Blazor.Grids.FilterType.Menu };

        internal Uri LastRequestUri { get; set; }
        internal Uri LastFailedRequestUri { get; set; }
        internal string GridContainerCssClass
        {
            get
            {
                var cssClasses = "";
                if (MudDialog != null) cssClasses += " shift-scrollable-content-wrapper";
                if (ActionUrlBroken) cssClasses += " disable-grid";
                return cssClasses;
            }
        }
        internal string GridCssClass
        { 
            get
            {
                var cssClass = "pa-6";
                if (IsReady) cssClass += " grid-loaded";
                if (MultiLineCells) cssClass += " multiline";
                return cssClass;
            }
        }

        public ShiftList()
        {
            GridId = "Grid" + Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        public event EventHandler<EventArgs>? OnDataBound;

        private async Task RecordClickHandler(RecordClickEventArgs<T> args)
        {
            if (OnRowClick.HasDelegate)
            {
                await OnRowClick.InvokeAsync(args);
            }
        }

        protected override void OnInitialized()
        {
            OnRequestFailed += HandleRequestFailed;
            OnRequestStarted += HandleRequestStarted;

            PageSize = SettingManager.Settings.ListPageSize ?? PageSize ?? DefaultAppSetting.ListPageSize;

            if (AutoGenerateColumns)
            {
                GenerateColumns();
            }
        }

        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            var result = await ShiftModal.Open(ComponentType, key, openMode, parameters);
            if (Grid != null && result != null && result.Canceled != true)
            {
                await Grid.Refresh();
            }
            return result;
        }

        internal void RestoreColumnsDefaultVisiblity(ColumnChooserFooterTemplateContext ctx)
        {
            SettingManager.SetHiddenColumns(GetListIdentifier(), new List<string>());
            NavManager.NavigateTo(NavManager.Uri, true);
        }

        internal async Task ChooseVisibleColumns(ColumnChooserFooterTemplateContext ctx)
        {
            var visibles = ctx.Columns.Where(x => x.Visible).Select(x => x.HeaderText).ToArray();
            var hiddens = ctx.Columns.Where(x => !x.Visible).Select(x => x.HeaderText).ToArray();

            await ctx.CancelAsync();

            await Grid!.ShowColumnsAsync(visibles);
            await Grid!.HideColumnsAsync(hiddens);

            SettingManager.SetHiddenColumns(GetListIdentifier(), hiddens.ToList());

        }

        internal async Task OnLoadHandler()
        {
            if (Grid != null && !DisableColumnChooser)
            {
                var hiddenColumns = SettingManager.GetHiddenColumns(GetListIdentifier()).ToArray();
                var visibleColumns = Grid
                    .Columns
                    .Where(x => !hiddenColumns.Contains(x.HeaderText) && x.Visible)
                    .Select(x => x.HeaderText)
                    .ToArray();
                try
                {
                    await Grid.HideColumnsAsync(hiddenColumns);
                    await Grid.ShowColumnsAsync(visibleColumns);
                }
                catch (Exception)
                {
                    SettingManager.SetHiddenColumns(GetListIdentifier(), new());
                }
            }
        }

        private void ErrorHandler(FailureEventArgs args)
        {
            var shouldRefresh = false;

            this.Grid?.Columns
                .Where(column => column.IsForeignColumn() && column.Template == null)
                .ToList()
                .ForEach(column =>
                {
                    if (LastFailedRequestUri.AbsoluteUriWithoutQuery() == column.DataManager.Url)
                    {
                        var type = column.GetType().GetGenericArguments().First();

                        Type genericClass = typeof(List<>);
                        Type constructedClass = genericClass.MakeGenericType(type);
                        var created = Activator.CreateInstance(constructedClass);

                        var colType = column.GetType();
                        var prop = colType.GetProperty("ForeignDataSource");

                        if (prop != null)
                        {
                            column.Template = (item) => UnableToLoadDataTemplate;

                            prop.SetValue(column, created);
                            shouldRefresh = true;
                        }
                    }
                });

            if (shouldRefresh)
            {
                this.Grid?.Refresh();
            }
            else
            {
                ActionUrlBroken = true;
                MsgService.Error(Loc["GetItemListError"], Loc["GetItemListError"], args.Error.ToString());
            }
        }

        public void DataBoundHandler()
        {
            if (!IsReady)
            {
                IsReady = true;
            }

            if (OnDataBound != null)
            {
                OnDataBound(this, new EventArgs());
            }
        }

        public async Task<SelectedItems> GetSelectedItems()
        {
            var AllSelected = await JsRuntime.InvokeAsync<bool>("GridAllSelected", this.Grid!.ID);

            var result = new SelectedItems
            {
                All = AllSelected,
                Query = LastRequestUri?.Query,
            };

            if (!result.All)
            {
                result.Items = this.Grid.SelectedRecords.Select(x => (object)x.ID).ToList();
            }

            return result;
        }

        public async Task ViewAddItem(object? key = null)
        {
            if (ComponentType != null)
            {
                var result = await OpenDialog(ComponentType, key, ModalOpenMode.Popup, this.AddDialogParameters);
                await OnFormClosed.InvokeAsync(result?.Data);
            }
        }

        public async Task PrintList()
        {
            if (this.Grid != null)
            {
                MsgService.Info(Loc["PrintMessage"]);
                await this.Grid.PrintAsync();
                return;
            }

            MsgService.Error(Loc["PrintFailed"]);
        }

        public async Task DownloadList(DownloadType type)
        {
            if (this.Grid == null)
            {
                MsgService.Error(Loc["DownloadFailed"]);
                return;
            }
            switch (type)
            {
                case DownloadType.CSV:
                    await this.Grid.ExportToCsvAsync();
                    break;
                case DownloadType.PDF:
                    await this.Grid.ExportToPdfAsync();
                    break;
                case DownloadType.Excel:
                    await this.Grid.ExportToExcelAsync();
                    break;
            }
        }

        private void ReloadGrid()
        {
            if (Grid != null)
            {
                ActionUrlBroken = false;
                Grid.Refresh();
            }
        }

        private void CloseDialog()
        {
            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
            }
        }

        internal async Task GoToPage(int page)
        {
            if (Grid!.PageSettings.CurrentPage != page)
            {
                await Grid!.GoToPageAsync(page);
            }
        }

        internal void PageSizeChangeHandler(int size)
        {
            PageSize = size;
            SettingManager.SetListPageSize(size);
        }


        public string GetListIdentifier()
        {
            return $"{Action}_{typeof(T).Name}";
        }

        internal async Task ChooseColumn()
        {
            await Grid!.OpenColumnChooserAsync(null, 0);
        }

        public enum DownloadType
        {
            CSV,
            PDF,
            Excel,
        }

        internal void GenerateColumns()
        {
            var properties = typeof(T)
                .GetProperties()
                .Where(x => !DefaultExcludedColumns.Contains(x.Name, StringComparer.CurrentCultureIgnoreCase))
                .Where(x => !ExcludedColumns.Contains(x.Name, StringComparer.CurrentCultureIgnoreCase));

            var complexColumns = new List<string>();

            foreach (var prop in properties)
            {
                var column = new ListColumn();

                column.Label = prop.Name;
                column.Field = GetFieldName(prop);

                if (!IsSystemType(prop.PropertyType) && prop.PropertyType.IsClass)
                {
                    complexColumns.Add(prop.Name);
                    column.IsComplex = true;
                }

                GeneratedColumns.Add(column);
            }

            Query.Expand(complexColumns);
        }

        internal string GetFieldName(PropertyInfo property)
        {
            var field = property.Name;
            // try getting the complex field name when field is complex type
            if (!IsSystemType(property.PropertyType) && property.PropertyType.IsClass)
            {
                var childProp = property
                    .PropertyType
                    .GetProperties()
                    .FirstOrDefault(x => x.PropertyType.Name == "String" && x.Name != nameof(ShiftEntityDTOBase.ID));

                if (!string.IsNullOrWhiteSpace(childProp?.Name))
                {
                    field = $"{property.Name}.{childProp.Name}";
                }
            }
            return field;
        }

        internal static bool IsSystemType(Type type)
        {
            return type.Namespace == "System";
        }

        private void HandleRequestFailed(object? sender, UriEventArgs args)
        {
            if (args.Uri != null)
            {
                LastFailedRequestUri = args.Uri;
            }
        }

        private void HandleRequestStarted(object? sender, UriEventArgs args)
        {
            if (args.Uri != null && args.Uri.AbsoluteUriWithoutQuery() == Grid.DataManager.Url)
            {
                LastRequestUri = args.Uri;
            }
        }

        void IDisposable.Dispose()
        {
            OnRequestFailed -= HandleRequestFailed;
            OnRequestStarted -= HandleRequestStarted;
        }
    }
}
