using Microsoft.AspNetCore.Components;
using MudBlazor;
using Syncfusion.Blazor.Grids;
using System.Reflection;
using Syncfusion.Blazor.Data;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using Microsoft.Extensions.Localization;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftList<T> : ComponentBase
        where T : ShiftEntityDTOBase, new() 
    {
        [Inject] private MessageService MsgService { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [Inject] protected HttpClient HttpClient { get; set; }

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
        public int PageSize { get; set; } = 10;

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
        ///     To set the Action Column's fixed width.
        /// </summary>
        [Parameter]
        public string ActionColumnWidth { get; set; } = "150";
        
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
        public List<IStringLocalizer> Localizers { get; set; } = new();

        [Parameter]
        public EventCallback<RecordClickEventArgs<T>> OnRowClick { get; set; }

        public SfGrid<T>? Grid;
        private CustomMessageHandler MessageHandler = new();
        internal List<ShiftColumn> GeneratedColumns = new();
        internal readonly List<string> DefaultExcludedColumns = new() { nameof(ShiftEntityDTOBase.ID), "Revisions" };
        internal int[] PageSizes = new int[] { 5, 10, 50, 100, 250, 500 };
        internal string GridContainerCssClass
        {
            get
            {
                var cssClasses = "";
                cssClasses += MudDialog != null ? "shift-scrollable-content-wrapper" : "";
                cssClasses += ActionUrlBroken ? " disable-grid" : "";
                return cssClasses;
            }
        }

        internal bool RenderAddButton => !(DisableAdd || ComponentType == null);
        internal bool ActionUrlBroken = false;
        internal bool IsReady = false;

        public ShiftList()
        {
            HttpClient = new HttpClient(MessageHandler);
        }

        private async Task RecordClickHandler(RecordClickEventArgs<T> args)
        {
            if (OnRowClick.HasDelegate)
            {
                await OnRowClick.InvokeAsync(args);
            }
        }

        protected override void OnInitialized()
        {
            if (SettingManager.Settings.ListPageSize != null)
            {
                PageSize = SettingManager.Settings.ListPageSize.Value;
            };

            if (AutoGenerateColumns)
            {
                GenerateColumns();
            }
        }

        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            var result = await ShiftModal.Open(ComponentType, key, openMode, parameters);

            if (Grid != null)
            {
                await Grid.Refresh();
            }
            return result;
        }

        private void ErrorHandler(FailureEventArgs args)
        {
            ActionUrlBroken = true;
            MsgService.Error("Error getting list of items", "Error getting list of items", args.Error.ToString());
        }

        public async Task<SelectedItems> GetSelectedItems()
        {
            var AllSelected = await JsRuntime.InvokeAsync<bool>("GridAllSelected", this.Grid!.ID);

            var result = new SelectedItems
            {
                All = AllSelected,
                Query = this.MessageHandler.Query,
            };

            if (!result.All)
            {
                result.Items = this.Grid.SelectedRecords.Select(x => (object)x.ID).ToList();
            }

            return result;
        }

        public async Task AddItem()
        {
            if (ComponentType != null)
            {
                await OpenDialog(ComponentType, null, ModalOpenMode.Popup, this.AddDialogParameters);
            }
        }

        public async Task PrintList()
        {
            if (this.Grid != null)
            {
                MsgService.Info("Opening Print Window, this might take a while");
                await this.Grid.PrintAsync();
                return;
            }

            MsgService.Error("Could not initiate print action");
        }

        public async Task DownloadList(DownloadType type)
        {
            if (this.Grid == null)
            {
                MsgService.Error("Could not initiate download action");
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

        internal async Task GoToPage(double page)
        {
            if (Grid!.PageSettings.CurrentPage != page)
            {
                await Grid!.GoToPageAsync(page);
            }
        }

        internal void PageSizeChangeHandler(int size)
        {
            PageSize = size;

            var pageSize = SettingManager.Settings.ListPageSize;

            if (pageSize == null || pageSize != size)
            {
                SettingManager.SetListPageSize(size);
            }
        }

        public void OnDataBoundHandler(BeforeDataBoundArgs<T> args)
        {
            if (!IsReady)
            {
                IsReady = true;
            }
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
                var column = new ShiftColumn();

                column.Label = GetLocalizedColumnLabel(prop.Name);
                column.Field = GetFieldName(prop);

                if (!IsSystemType(prop.PropertyType) && prop.PropertyType.IsClass)
                {
                    complexColumns.Add(prop.Name);
                }

                GeneratedColumns.Add(column);
            }

            Query.Expand(complexColumns);
        }

        internal string GetLocalizedColumnLabel(string name)
        {
            var label = name;
            foreach (var localizer in Localizers)
            {
                label = localizer[name];
                if (label != name) break;
            }
            return label;
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
    }

    public class ShiftColumn
    {
        public string Label { get; set; }
        public string Field { get; set; }
    }
}
