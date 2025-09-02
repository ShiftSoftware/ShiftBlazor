using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.OData.Client;
using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Components.Print;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    [CascadingTypeParameter(nameof(T))]
    public partial class ShiftList<T> : IODataRequestComponent<T>, IShortcutComponent, ISortableComponent, IFilterableComponent, IShiftList where T : ShiftEntityDTOBase, new()
    {
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] public HttpClient HttpClient { get; private set; } = default!;
        [Inject] ShiftModal ShiftModal { get; set; } = default!;
        [Inject] public ShiftBlazorLocalizer Loc  { get; private set; } = default!;
        [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
        [Inject] public SettingManager SettingManager { get; private set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] MessageService MessageService { get; set; } = default!;
        [Inject] NavigationManager NavigationManager { get; set; } = default!;
        [Inject] PrintService PrintService { get; set; } = default!;

        [CascadingParameter]
        protected IMudDialogInstance? MudDialog { get; set; }

        /// <summary>
        /// To check whether this list is currently embeded inside a form component.
        /// </summary>
        [CascadingParameter(Name = FormHelper.ParentReadOnlyName)]
        public bool? ParentReadOnly { get; set; }

        [CascadingParameter(Name = FormHelper.ParentDisabledName)]
        public bool? ParentDisabled { get; set; }

        /// <summary>
        /// The current fetched items, this will be fetched from the OData API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public List<T>? Values { get; set; }

        /// <summary>
        /// An event triggered when the state of Values has changed.
        /// </summary>
        [Parameter]
        public EventCallback<List<T>> ValuesChanged { get; set; }

        /// <summary>
        /// The OData EntitySet Name.
        /// </summary>
        [Parameter]
        public string? EntitySet { get; set; }

        [Parameter]
        public string? Endpoint { get; set; }

        /// <summary>
        /// The OData api endpoint.
        /// </summary>
        [Parameter]
        public string? BaseUrl { get; set; }

        /// <summary>
        /// The OData api endpoint config key.
        /// </summary>
        [Parameter]
        public string? BaseUrlKey { get; set; }

        /// <summary>
        /// The type of the component to be opened as a dialog when clicking on Add or the Action button.
        /// If empty, 'Add' and 'Action button' column will be hidden.
        /// </summary>
        [Parameter]
        public Type? ComponentType { get; set; }

        /// <summary>
        /// To pass additional parameters to the 'ShiftFormContainer' component.
        /// </summary>
        [Parameter]
        public Dictionary<string, object>? AddDialogParameters { get; set; }

        /// <summary>
        /// Enable row selection.
        /// </summary>
        [Parameter]
        public bool EnableSelection { get; set; }

        /// <summary>
        /// Enable Virtualization and disable Paging.
        /// 'Height' paramater should have a valid value when this is enabled.
        /// </summary>
        [Parameter]
        public bool EnableVirtualization { get; set; }

        /// <summary>
        /// Sets the css height property for the Datagrid.
        /// </summary>
        [Parameter]
        public string Height { get; set; } = string.Empty;

        /// <summary>
        /// The title used for the form and the browser tab title.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

        /// <summary>
        /// The css value used for the toolbar's 'background-color'.
        /// Only RGB and Hex values work with 'NavIconFlatColor'.
        /// </summary>
        [Parameter]
        public string? NavColor { get; set; }

        /// <summary>
        /// When true, the toolbar's text color will be in white or black, depending on the contrast of the background.
        /// </summary>
        [Parameter]
        public bool NavIconFlatColor { get; set; }

        /// <summary>
        /// The icon displayed before the Form Title. (SVG string)
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = @Icons.Material.Filled.List;

        /// <summary>
        /// Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarStartTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarEndTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the controls section of the header toolbar.
        /// This section is only visible when the form is opened in a dialog.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarControlsTemplate { get; set; }

        /// <summary>
        /// When true, the header toolbar will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableHeaderToolbar { get; set; }

        /// <summary>
        /// When true, the Action Column will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableActionColumn { get; set; } = true;

        /// <summary>
        /// When true, the Delete Filter will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableDeleteFilter { get; set; }

        /// <summary>
        /// When true, the Column Chooser will not be rendered.
        /// </summary>
        [Parameter]
        [Obsolete("ColumnChooser is replaced with GridEditor")]
        public bool DisableColumnChooser { get; set; }

        /// <summary>
        /// Disables the column editor menu.
        /// </summary>
        [Parameter]
        public bool DisableGridEditor { get; set; }

        /// <summary>
        /// When true, the 'Add' button will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableAdd { get; set; }

        /// <summary>
        /// When true, the form is more compact and smaller.
        /// </summary>
        [Parameter]
        public bool Dense { get; set; }

        /// <summary>
        /// Fires when a row is clicked, sends 'DataGridRowClickEventArgs<T>' as argument.
        /// </summary>
        [Parameter]
        public EventCallback<ShiftEvent<DataGridRowClickEventArgs<T>>> OnRowClick { get; set; }

        /// <summary>
        /// Fires when form is closed, sends the form data when form is saved and null if cancelled.
        /// </summary>
        [Parameter]
        public EventCallback<object?> OnFormClosed { get; set; }

        /// <summary>
        /// Fires when DataGrid is loaded.
        /// </summary>
        [Parameter]
        public EventCallback OnLoad { get; set; }

        [Parameter]
        [Obsolete("Use OnSelectStateChanged instead")]
        public EventCallback<HashSet<T>> OnSelectedItemsChanged { get; set; }
        
        [Parameter]
        public EventCallback<SelectState<T>> OnSelectStateChanged { get; set; }

        [Parameter]
        public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }
        [Parameter]
        public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }
        [Parameter]
        public Func<Exception, ValueTask<bool>>? OnError { get; set; }
        [Parameter]
        public Func<ODataDTO<T>?, ValueTask<bool>>? OnResult { get; set; }

        [Parameter]
        public RenderFragment<ListChildContext<T>>? ChildContent { get; set; }

        /// <summary>
        /// Whether to render or not render 'Entity ID' column
        /// </summary>
        [Parameter]
        public bool ShowIDColumn { get; set; } = true;

        /// <summary>
        /// The number of items to be displayed per page.
        /// </summary>
        [Parameter]
        public int? PageSize { get; set; }

        /// <summary>
        /// Enable and show Export button.
        /// </summary>
        [Parameter]
        public bool EnableExport { get; set; }

        /// <summary>
        /// Disable sticky header, works only with Height and EnableVirtualization.
        /// </summary>
        [Parameter]
        public bool DisableStickyHeader { get; set; }

        /// <summary>
        /// When true, the pagination will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisablePagination { get; set; }

        /// <summary>
        /// Disable column sorting.
        /// </summary>
        [Parameter]
        public bool DisableSorting { get; set; }

        /// <summary>
        /// Disables a feature where you can select multiple columns to sort. (Ctrl + Click)
        /// </summary>
        [Parameter]
        public bool DisableMultiSorting { get; set; }

        /// <summary>
        /// Disable column filtering.
        /// </summary>
        [Parameter]
        public bool DisableFilters { get; set; }

        /// <summary>
        /// Used to override any element in the Action column.
        /// </summary>
        [Parameter]
        public RenderFragment<CellContext<T>>? ActionsTemplate { get; set; }

        /// <summary>
        /// Give the form window an outline and disable elevation.
        /// </summary>
        [Parameter]
        public bool Outlined { get; set; }

        [Parameter]
        public Dictionary<string, SortDirection> Sort { get; set; } = [];

        [Parameter]
        public Action<ODataFilterGenerator>? Filter { get; set; }

        /// <summary>
        /// When true, row-click also toggles the checkbox state
        /// </summary>
        [Parameter]
        public bool SelectOnRowClick { get; set; } = false;

        [Parameter]
        public bool EnableFilterPanel { get; set; }

        [Parameter]
        public bool FilterPanelDefaultOpen { get; set; }

        [Parameter]
        public bool FilterImmediate { get; set; }

        [Parameter]
        public RenderFragment? FilterTemplate {  get; set; }

        [Parameter]
        public bool EnablePrintColumn { get; set; }

        [Parameter]
        public PrintFormConfig? PrintConfig { get; set; }

        [Parameter]
        public bool DisableReloadButton { get; set; }

        [Parameter]
        public string? SortedColgroupStyle { get; set; }

        [Parameter]
        public bool HighlightSortedColumn { get; set; }

        public Uri? CurrentUri { get; set; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = [];
        public bool IsEmbed { get; private set; } = false;
        [Obsolete("Use SelectState.Items instead")]
        public HashSet<T> SelectedItems => SelectState.Items.ToHashSet();
        [Obsolete("Use SelectState.All instead")]
        public bool IsAllSelected => SelectState.All;
        public readonly SelectState<T> SelectState = new();

        internal Size IconSize = Size.Medium;
        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;
        internal bool RenderAddButton = false;
        internal int SelectedPageSize;
        internal int[] PageSizes = [ 5, 10, 50, 100, 250, 500 ];
        internal bool? deleteFilter = false;
        internal string? ErrorMessage;
        private ITypeAuthService? TypeAuthService;
        private string ToolbarStyle = string.Empty;
        internal SortMode SortMode = SortMode.Multiple;
        public ODataFilterGenerator ODataFilters { get; private set; } = new ODataFilterGenerator(true);
        private string PreviousFilters = string.Empty;
        private bool ReadyToRender = false;
        private bool IsModalOpen = false;
        private bool IsGridEditorOpen = false;
        private bool IsDeleteColumnHidden = true;
        private string GridEditorHeight => string.IsNullOrWhiteSpace(Height) ? "350px" : $"calc({Height} - 50px)";
        public Dictionary<Guid, FilterModelBase> Filters { get; set; } = [];
        private Debouncer Debouncer { get; set; } = new Debouncer();
        private bool IsFilterPanelOpen { get; set; }
        public HashSet<Guid> ActiveOperations { get; set; } = [];
        private CancellationTokenSource? ReloadBlockTokenSource;
        public bool IsLoading { get; set; }

        private TaskCompletionSource<GridData<T>> IndefiniteReloadTask = new();
        private CancellationTokenSource? ReloadCancellationTokenSource { get; set; }

        protected string GetRowClassname(T item, int colIndex) =>
            new CssBuilder()
                .AddClass("is-deleted", item.IsDeleted)
                .Build();

        protected string SortedColgroupStylename =>
            new StyleBuilder()
                .AddStyle("background", "rgba(var(--mud-palette-primary-rgb), 0.25)", string.IsNullOrWhiteSpace(SortedColgroupStyle))
                .AddStyle(SortedColgroupStyle)
                .Build();

        private List<Column<T>> DraggableColumns
        {
            get
            {
                if (DataGrid == null)
                {
                    return [];
                }

                if (EnableSelection)
                {
                    var count = DataGrid.RenderedColumns.Count;
                    return DataGrid.RenderedColumns.GetRange(1, count - 1);
                }

                return DataGrid.RenderedColumns;
            }
        }

        internal Func<GridState<T>, Task<GridData<T>>>? ServerData = default;

        private MudDataGrid<T>? _DataGrid;
        public MudDataGrid<T>? DataGrid
        {
            get => _DataGrid;
            set
            {
                _DataGrid = value;
                OnDataGridLoad();
                OnLoad.InvokeAsync();
            }
        }

        public bool ExportIsInProgress { get; private set; } = false;

        protected override void OnInitialized()
        {
            IsEmbed = ParentDisabled != null || ParentReadOnly != null;

            if (!IsEmbed)
            {
                IShortcutComponent.Register(this);
            }

            if (Values == null && EntitySet == null)
            {
                throw new ArgumentNullException($"{nameof(Values)} and {nameof(EntitySet)} are null");
            }

            ShiftBlazorEvents.OnModalClosed += ShiftBlazorEvents_OnModalClosed;
            TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();

            if (EntitySet != null)
            {
                string? url = IRequestComponent.GetPath(this);
                
                QueryBuilder = OData
                    .CreateNewQuery<T>(EntitySet, url)
                    .IncludeCount();
            }

            RenderAddButton = !(DisableAdd || ComponentType == null || (TypeAuthAction != null && TypeAuthService?.Can(TypeAuthAction, Access.Write) != true));
            IconSize = Dense ? Size.Medium : Size.Large;
            ToolbarStyle = $"{ColorHelperClass.GetToolbarStyles(NavColor, NavIconFlatColor)}border: 0;";
            ServerData = Values == null
                ? new Func<GridState<T>, Task<GridData<T>>>(ServerReload)
                : default;
            SortMode = DisableSorting
                        ? SortMode.None
                        : DisableMultiSorting
                            ? SortMode.Single
                            : SortMode.Multiple;

            if (PageSize != null && !PageSizes.Any(x => x == PageSize))
            {
                PageSizes = PageSizes.Append(PageSize.Value).Order().ToArray();
            }

            if (Values != null)
            {
                SelectState.Total = Values.Count;
            }

            SelectedPageSize = SettingManager.Settings.ListPageSize ?? PageSize ?? DefaultAppSetting.ListPageSize;
            IsFilterPanelOpen = SettingManager?.GetFilterPanelState() ?? FilterPanelDefaultOpen;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                if (!DisableGridEditor)
                {
                    var columnStates = SettingManager.GetColumnState(GetListIdentifier());
                    HideDisabledColumns(columnStates);
                    MakeColumnsSticky(columnStates);
                    ReorderColumns(columnStates);
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) => await JsRuntime.InvokeVoidAsync("fixStickyColumn", $"Grid-{Id}");

        protected override bool ShouldRender()
        {
            return ReadyToRender;
        }

        protected override void OnParametersSet()
        {
            if (Filter != null)
            {
                var filter = new ODataFilterGenerator(true, Id);
                Filter.Invoke(filter);
                ODataFilters.Add(filter);
            }

            var currentFilters = ODataFilters.ToString();

            if (currentFilters != PreviousFilters)
            {
                PreviousFilters = currentFilters;
                SelectState.Clear();
                DataGrid?.ReloadServerData();
            }

            if (DataGrid == null)
            {
                return;
            }

            // Should only check on DisableActionColumn paramater change
            if (DisableActionColumn)
            {
                var actionColumn = DataGrid.RenderedColumns.LastOrDefault(x => x.Title == Loc["ActionsColumnHeaderText"]);
                DataGrid.RenderedColumns.Remove(actionColumn);
            }

            if (DataGrid.Virtualize != EnableVirtualization && Values == null)
            {
                DataGrid.ReloadServerData();
            }
        }

        /// <summary>
        /// Opens a dialog form to create a new item or edit an existing one.
        /// </summary>
        /// <param name="key">The unique ID of the item to be edited. If null, the form will be in 'Create' mode.</param>
        /// <returns>The data of the created or edited item after the dialog is closed. Returns null if the form is not saved.</returns>
        public async Task ViewAddItem(object? key = null)
        {
            if (ComponentType != null)
            {
                var result = await OpenDialog(ComponentType, key, ModalOpenMode.Popup, this.AddDialogParameters);
                await OnFormClosed.InvokeAsync(result?.Data);
            }
        }

        /// <summary>
        /// Opens a dialog window.
        /// </summary>
        /// <param name="ComponentType">The type of component to be opened.</param>
        /// <param name="key">The unique ID of the item to be opened.</param>
        /// <param name="openMode">Specifies how the dialog window opens.</param>
        /// <param name="parameters">The parameters to be passed to the component.</param>
        /// <returns>A DialogResult object representing the outcome of the dialog.</returns>
        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null)
        {
            IsModalOpen = true;
            var result = await ShiftModal.Open(ComponentType, key, openMode, parameters);
            if (result != null && result.Canceled != true)
            {
                await DataGrid!.ReloadServerData();
            }
            IsModalOpen = false;
            return result;
        }

        /// <summary>
        /// Asynchronously sets the sorting configuration for the data grid.
        /// </summary>
        /// <param name="field">The field by which the data should be sorted.</param>
        /// <param name="sortDirection">The direction of sorting (ascending or descending).</param>
        public async Task SetSortAsync(string field, SortDirection sortDirection)
        {
            if (DataGrid != null)
            {
                await DataGrid.SetSortAsync(field, sortDirection, null);
            }
        }

        /// <summary>
        /// Sets the sorting configuration for the data grid.
        /// </summary>
        /// <param name="field">The field by which the data should be sorted.</param>
        /// <param name="sortDirection">The direction of sorting (ascending or descending).</param>
        public void SetSort(string field, SortDirection sortDirection)
        {
            var sort = new SortDefinition<T>(field, sortDirection == SortDirection.Descending, DataGrid?.SortDefinitions.Count ?? 0, null);
            DataGrid?.SortDefinitions.Add(field, sort);
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Adds a filter to the data grid.
        /// </summary>
        /// <param name="field">The field to apply the filter on.</param>
        /// <param name="op">The comparison operator for the filter (e.g., Equal, GreaterThan).</param>
        /// <param name="value">The value to compare against for the filter.</param>
        public void AddFilter(Guid id, string field, ODataOperator op, object? value = null)
        {
            ODataFilters.Add(field, op, value, id);
        }

        public void GridStateHasChanged()
        {
            StateHasChanged();
        }

        public async ValueTask HandleShortcut(KeyboardKeys key)
        {
            switch (key)
            {
                case KeyboardKeys.Escape:
                    CloseDialog();
                    break;
                case KeyboardKeys.KeyA:
                    if (RenderAddButton)
                        await ViewAddItem();
                    break;
                case KeyboardKeys.KeyE:
                    if (EnableExport)
                        await ExportList();
                    break;
                case KeyboardKeys.KeyC:
                    if (!DisableGridEditor)
                        OpenGridEditor();
                    break;
            }
        }

        private async Task<GridData<T>> ServerReload(GridState<T> state)
        {
            IsLoading = true;
            StateHasChanged();

            // Check if there are any active operations,
            // if so, wait for them to finish before proceeding,
            // only if it is the first request.
            // Operations could be things like Filter, Sort...
            if (!ReadyToRender && ActiveOperations.Count > 0)
            {
                try
                {
                    ReloadBlockTokenSource = new CancellationTokenSource();
                    await Task.Delay(300, ReloadBlockTokenSource.Token);
                }
                catch (Exception) { }

                ReloadBlockTokenSource?.Dispose();
                ReloadBlockTokenSource = null;
            }

            ReloadCancellationTokenSource?.Cancel();
            ReloadCancellationTokenSource?.Dispose();
            ReloadCancellationTokenSource = new CancellationTokenSource();
            var cts = ReloadCancellationTokenSource;
            
            GridData<T> gridData = new();
            bool preventDefault = false;
            ErrorMessage = null;

            try
            {
                // Save current PageSize as user preference 
                if (state.PageSize != SelectedPageSize)
                {
                    SettingManager.SetListPageSize(state.PageSize);
                    SelectedPageSize = state.PageSize;
                }

                var url = BuildODataUrl(state);
                if (string.IsNullOrWhiteSpace(url))
                    throw new Exception(Loc["DataReadUrlError"]);

                CurrentUri = new Uri(url!);
                var content = await IODataRequestComponent<T>.GetFromJsonAsync(this, CurrentUri, cts.Token);

                if (content == null)
                {
                    preventDefault = true;
                    return gridData;
                }

                gridData = new GridData<T>
                {
                    Items = content.Value ?? [],
                    TotalItems = (int?)content.Count ?? content.Value?.Count ?? 0,
                };

                SelectState.Total = gridData.TotalItems;

                //await OnFetch.InvokeAsync(content.Value);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // the only way to cancel the request is when the user calls another
                // ServerReload method while the previous request is still in progress.
                // We return an indefinite task to prevent MudBlazor from stopping the loading animation.
                // We later resolve this task when the request is completed.
                return await IndefiniteReloadTask.Task;
            }
            catch (JsonException e)
            {
                if (OnError != null && await OnError.Invoke(e))
                {
                    return gridData;
                }

                ErrorMessage = Loc["DataParseError"];
                MessageService.Error(Loc["DataReadError"], e.InnerException?.Message, e.Message, buttonText: Loc["DropdownViewButtonText"]);
            }
            catch (Exception e)
            {
                if (OnError != null && await OnError.Invoke(e))
                {
                    return gridData;
                }

                ErrorMessage = e.Message;
                MessageService.Error(e.Message, e.Message, e.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }
            finally
            {
                ReadyToRender = true;

                if (ReferenceEquals(cts, ReloadCancellationTokenSource))
                {
                    IndefiniteReloadTask.SetResult(gridData);
                    IndefiniteReloadTask = new();
                    IsLoading = false;

                    if (!preventDefault)
                    {
                        ShiftBlazorEvents.TriggerOnBeforeGridDataBound(new KeyValuePair<Guid, List<object>>(Id, gridData.Items.ToList<object>()));
                    }
                    StateHasChanged();
                }


            }

            return gridData;
        }

        private DataServiceQuery<T> BuildSort(GridState<T> state, DataServiceQuery<T> builder)
        {
            try
            {
                // Convert MudBlazor's SortDefinitions to OData query
                if (state.SortDefinitions.Count > 0)
                {
                    var sortList = state.SortDefinitions.ToODataFilter();
                    builder = builder.AddQueryOption("$orderby", string.Join(',', sortList));
                }
            }
            catch (Exception e)
            {
                ErrorMessage = Loc["GridSortError"];
                MessageService.Error(ErrorMessage, e.Message, e!.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }

            return builder;
        }

        private DataServiceQuery<T> BuildFilter(GridState<T> state, DataServiceQuery<T> builder)
        {
            // Remove multiple empty filters but keep the last added empty filter
            // state.FilterDefinitions is same as DataGrid!.FilterDefinitions
            var emptyFields = state.FilterDefinitions
                .Where(x => x.Value == null && x.Operator != FilterOperator.String.Empty && x.Operator != FilterOperator.String.NotEmpty)
                .ToList();

            for (var i = 0; i < Math.Max(0, emptyFields.Count - 1); i++)
            {
                DataGrid!.FilterDefinitions.Remove(emptyFields[i]);
            }

            try
            {
                var filterPanel = Filters.Select(x => x.Value.ToODataFilter().ToString()).Where(x => !string.IsNullOrWhiteSpace(x));

                // Convert MudBlazor's FilterDefinitions to OData query
                var userFilters = state.FilterDefinitions.ToODataFilter().Select(x => string.Join(" or ", x));

                var filterList = new List<string>();
                filterList.AddRange(userFilters);
                filterList.AddRange(filterPanel);

                if (ODataFilters.Count > 0)
                    filterList.Add(ODataFilters.ToString());

                if (filterList.Count > 0)
                {
                    var filterQueryString = $"({string.Join(") and (", filterList)})";
                    builder = builder.AddQueryOption("$filter", filterQueryString);
                    SelectState.Filter = filterQueryString;
                }
                else
                {
                    SelectState.Filter = null;
                }
            }
            catch (Exception e)
            {
                ErrorMessage = Loc["GridFilterError"];
                MessageService.Error(Loc["ShiftListFilterParseError"], e.Message, e!.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }

            return builder;
        }

        private string? BuildODataUrl(GridState<T> state)
        {
            var builder = QueryBuilder;

            builder = BuildSort(state, builder);
            builder = BuildFilter(state, builder);

            var builderQueryable = builder.AsQueryable();

            // apply pagination values
            if (!EnableVirtualization)
            {
                builderQueryable = builderQueryable
                    .Skip(state.Page * state.PageSize)
                    .Take(state.PageSize);
            }

            return builderQueryable.ToString();
        }

        private void ShiftBlazorEvents_OnModalClosed(object? sender, object? data)
        {
            // Use Shortcut components to find out if the datagrid is on top of the component list
            if (!IsModalOpen)
            {
                var beforeLastIndex = IShortcutComponent.Components.Count - 2;
                var compId = IShortcutComponent.Components.Keys.ElementAtOrDefault(beforeLastIndex);

                if (compId == Id && data != null)
                {
                    DataGrid!.ReloadServerData();
                }
            }
        }

        internal void OnDataGridLoad()
        {
            foreach (var sort in Sort)
            {
                SetSort(sort.Key, sort.Value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delete">
        /// true: only get deleted items.
        /// false: only get active items.
        /// null: get all.
        /// </param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal void FilterDeleted(bool? delete = null)
        {
            DataGrid!.FilterDefinitions.RemoveAll(x => x.Column!.PropertyName == nameof(ShiftEntityDTOBase.IsDeleted));

            switch (delete)
            {
                case true:
                    DataGrid!.FilterDefinitions.Add(CreateDeleteFilter());
                    break;
                case false:
                    DataGrid!.FilterDefinitions.Add(CreateDeleteFilter(false));
                    break;
                case null:
                    DataGrid!.FilterDefinitions.Add(CreateDeleteFilter());
                    DataGrid!.FilterDefinitions.Add(CreateDeleteFilter(false));
                    break;
            }

            _ = DataGrid?.ReloadServerData();
        }

        private FilterDefinition<T> CreateDeleteFilter(bool value = true)
        {
            return new FilterDefinition<T>
            {
                Column = DataGrid!.GetColumnByPropertyName<T>(nameof(ShiftEntityDTOBase.IsDeleted)),
                Operator = FilterOperator.String.Equal,
                Value = value,
            };
        }

        private void CloseDialog()
        {
            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
                IShortcutComponent.Remove(Id);
            }
        }

        private async Task RowClickHandler(DataGridRowClickEventArgs<T> args)
        {
            if (OnRowClick.HasDelegate)
            {
                if (await OnRowClick.PreventableInvokeAsync(args)) return;
            }

            if (SelectOnRowClick)
            {
                await SelectRow(args.Item);
            }
        }

        private string GetListIdentifier()
        {
            return $"{EntitySet}_{typeof(T).Name}";
        }

        #region Columns

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
        private async Task ColumnHiddenStateChanged(string name, bool hidden)
        {
            var col = DataGrid?.RenderedColumns.FirstOrDefault(x => (x.Hideable ?? DataGrid?.Hideable) == true && x.Title == name);
            if (col != null)
                col.Hidden = hidden;
            await SaveColumnState();
        }

        private void HideDisabledColumns(List<ColumnState> columnStates)
        {
            var columns = DataGrid?.RenderedColumns.Where(x => (x.Hideable ?? DataGrid?.Hideable) == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

            try
            {
                foreach (var item in columnStates)
                {
                    var column = columns.FirstOrDefault(x => x.Title == item.Title);

                    if (column?.Title == @Loc["IsDeletedColumnHeaderText"])
                    {
                        IsDeleteColumnHidden = !item.Visible;
                    } 
                    else if (column != null)
                    {
                        column.Hidden = !item.Visible;
                        _ = item.Visible == true
                            ? column?.ShowAsync()
                            : column?.HideAsync();
                    }
                }

                foreach (var item in columns)
                {
                    item.HiddenChanged = new EventCallback<bool>(this, async delegate(bool value) { await ColumnHiddenStateChanged(item.Title, value); });
                }
            }
            catch (Exception e)
            {
                MessageService.Error(Loc["HideDisabledColumnError"], e.Message, e.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }
        }

        private void MakeColumnsSticky(List<ColumnState> columnStates)
        {
            if (DataGrid == null || columnStates.Count == 0)
            {
                return;
            }

            foreach (var item in columnStates)
            {
                var column = DataGrid.RenderedColumns.FirstOrDefault(x => x.Title == item.Title);
                if (column != null)
                {
                    column.StickyLeft = column.StickyRight = item.Sticky;
                }
            }
        }

        private async Task ToggleSticky(Column<T> column, bool sticky)
        {
            column.StickyLeft = sticky;
            column.StickyRight = sticky;
            await SaveColumnState();
        }
#pragma warning restore BL0005

        private void ReorderColumns(List<ColumnState> columnStates)
        {
            // This methods needs some rework and testing
            if (DataGrid == null || columnStates.Count == 0)
            {
                return;
            }

            // reorder the columns
            var columnOrderByNames = columnStates.Select(x => x.Title).ToList();
            var reorderedColumns = columnOrderByNames
                .Select(x => DraggableColumns.FirstOrDefault(y => y.Title == x))
                .Where(x => x != null)
                .ToList();

            // add any missing columns in columnState
            reorderedColumns.AddRange(DraggableColumns.Where(col => !columnStates.Any(state => state.Title == col.Title)));

            // add select column at the beginning after reordering
            if (EnableSelection)
            {
                reorderedColumns.Insert(0, DataGrid.RenderedColumns.First());
            }

            DataGrid.RenderedColumns.Clear();
            DataGrid.RenderedColumns.AddRange(reorderedColumns);
        }

        private async Task ColumnOrderUpdated(MudItemDropInfo<Column<T>> dropItem)
        {
            if (DataGrid != null)
            {
                DataGrid.RenderedColumns.Remove(dropItem.Item);
                DataGrid.RenderedColumns.Insert(dropItem.IndexInZone, dropItem.Item);

                DropContainerHasChanged();

                await SaveColumnState();
            }

            return;
        }

        private async ValueTask SaveColumnState()
        {
            var columns = DataGrid?.RenderedColumns.Where(x => (x.Hideable ?? DataGrid?.Hideable) == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

            var columnStates = columns.Select(x => new ColumnState
            {
                Title = x.Title,
                Visible = !x.Hidden,
                Sticky = x.StickyLeft,
            }).ToList();

            if (columnStates.Any(x => x.Sticky))
            {
                await JsRuntime.InvokeVoidAsync("fixStickyColumn", $"Grid-{Id}");
            }

            SettingManager.SetColumnState(GetListIdentifier(), columnStates);
        }

        private void OpenGridEditor()
        {
            IsGridEditorOpen = true;
        }

        private void CloseGridEditor()
        {
            IsGridEditorOpen = false;
        }

        private void ResetColumnSettings()
        {
            SettingManager.SetColumnState(GetListIdentifier(), []);
            NavigationManager.Refresh(true);
        }

        #endregion

        internal async Task RerenderDataGrid()
        {
            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessForeignColumns<TForeign>(List<TForeign> items)
        {
            var foreignColumns = DataGrid!
                    .RenderedColumns
                    .Where(x => x.Class?.Contains("foreign-column") == true)
                    .Select(x => x as IForeignColumn);


            var entityType = typeof(T);

            var lockObject = new object();

            var tasks = foreignColumns
            .Where(column => column != null)
            .Select(async column =>
            {
                if (column is null)
                    return;

                var itemIds = IForeignColumn.GetForeignIds(column, items);
                var foreignData = await IForeignColumn.GetForeignColumnValues(column, itemIds, OData, HttpClient);
                var field = IForeignColumn.GetDataValueFieldName(column);

                var columnProperty = entityType.GetProperty(field);
                var foreignType = column.GetType().GetGenericArguments().Last();

                var attr = Misc.GetAttribute<ShiftEntityKeyAndNameAttribute>(foreignType);
                var foreignTextField = column.ForeignTextField ?? attr?.Text ?? "";

                var idProp = foreignType.GetProperty(nameof(ShiftEntityDTOBase.ID));
                var textProp = foreignType.GetProperty(foreignTextField);

                PropertyInfo? foriegnEntityProp = null;

                if (column.ForeignEntiyField is not null)
                {
                    foriegnEntityProp = entityType.GetProperty(column.ForeignEntiyField);
                }

                if (idProp == null || textProp == null || foreignData == null || columnProperty == null)
                    return;

                foreach (var row in items)
                {
                    var id = columnProperty.GetValue(row);

                    var foriegnDataMatch = foreignData.Value.FirstOrDefault(x => idProp.GetValue(x)?.ToString() == id?.ToString());

                    if (foriegnDataMatch != null)
                    {
                        lock (lockObject)
                        {
                            columnProperty.SetValue(row, textProp.GetValue(foriegnDataMatch));
                            foriegnEntityProp?.SetValue(row, foriegnDataMatch);
                        }
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        #region Export
        private async Task<Stream> GetStream(string url)
        {
            var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });

            try
            {
                if (!(res?.Value.Count > 1))
                    throw new InvalidOperationException("No Items found");

                await ProcessForeignColumns(res.Value);
            }
            catch (Exception e)
            {
                MessageService.Error(Loc["ShiftListForeignColumnError"], Loc["ShiftListForeignColumnError"], e.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }

            return GetStream(res?.Value);
        }

        private Stream GetStream(List<T>? items)
        {
            var stream = new MemoryStream();

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);

            if (items != null && items.Count > 0)
            {
                using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true))
                {
                    var csvWriter = new CsvWriter(streamWriter, config);

                    var columns = DataGrid!
                        .RenderedColumns
                        .Where(x =>
                        {
                            if (!x.Hidden)
                                return true;

                            var forceExportProp = x.GetType().GetProperty(nameof(PropertyColumnExtended<T, object>.ForceExportIfHidden));

                            if (forceExportProp != null)
                            {
                                var forceExportValue = forceExportProp.GetValue(x) as bool?;

                                return forceExportValue == true;
                            }

                            return false;
                        })
                        .Where(x => x.GetType().GetProperty("Property") != null);

                    // Write headers
                    foreach (var column in columns)
                    {
                        csvWriter.WriteField(column.Title);
                    }
                    csvWriter.NextRecord();

                    // Write rows
                    foreach (var item in items)
                    {
                        foreach (var column in columns)
                        {
                            // Get the column's Property parameter
                            var ColumnExpression = column.GetType().GetProperty("Property")?.GetValue(column);
                            if (ColumnExpression is LambdaExpression lambdaExpression)
                            {
                                // Compile and invoke the method so we can replicate the result shown on the DataGrid
                                var compiled = lambdaExpression.Compile();
                                try
                                {
                                    object? result = compiled.DynamicInvoke(item);

                                    if (result is DateTime dtValue)
                                    {
                                        csvWriter.WriteField(dtValue.ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                    else if (result is DateTimeOffset dtoValue)
                                    {
                                        csvWriter.WriteField(dtoValue.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                    else
                                    {
                                        csvWriter.WriteField(result);
                                    }
                                }
                                catch (Exception)
                                {
                                    csvWriter.WriteField(null);
                                }
                            }
                        }

                        csvWriter.NextRecord();
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);
            }

            return stream;
        }

        internal async Task ExportList()
        {
            if (ExportIsInProgress)
            {
                return;
            }

            this.ExportIsInProgress = true;

            var name = Title != null && ExportTitleRegex().IsMatch(Title)
                ? Title
                : EntitySet ?? typeof(T).Name;

            name = ExportTitleRegex().Replace(name, "");
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(name) ? $"file_{date}.csv" : $"{name}_{date}.csv";

            Stream stream;

            if (CurrentUri == null)
            {
                stream = GetStream(Values);
            }
            else
            {
                var url = ExportUrlRegex().Replace(CurrentUri.AbsoluteUri, "");
                stream = await GetStream(url);
            }

            if (stream.Length > 0)
            {
                using var streamRef = new DotNetStreamReference(stream: stream);
                await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }

            this.ExportIsInProgress = false;
        }

        #endregion

        internal async Task SelectRow(T item)
        {
            // search the selected list, if we find an item with the current item id, then remove it
            var removedItems = SelectState.Items.RemoveAll(x => x.ID == item.ID);

            // if no items were removed from the list, it means we want to add it to the list
            if (removedItems == 0 && !SelectState.All)
            {
                SelectState.Items.Add(item);
            }

            SelectState.All = false;
            await OnSelectStateChanged.InvokeAsync(SelectState);
        }

        internal async Task SelectAll(bool selectAll)
        {
            SelectState.All = selectAll;
            if (selectAll)
            {
                SelectState.Items.Clear();
            }
            else
            {
                SelectState.Clear();
            }
            await OnSelectStateChanged.InvokeAsync(SelectState);
        }

        public void ToggleFilterPanel()
        {
            IsFilterPanelOpen = SettingManager?.SetFilterPanelState(!IsFilterPanelOpen) ?? !IsFilterPanelOpen;
        }

        public void Reload()
        {
            if (ReloadBlockTokenSource?.IsCancellationRequested == false)
            {
                ReloadBlockTokenSource.Cancel();
            }
            else
            {
                Debouncer.Debounce(100, DataGrid!.ReloadServerData);
            }
        }

        private async Task PrintItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            var url = IRequestComponent.GetPath(this).AddUrlPath(EntitySet);
            if (PrintConfig == null)
            {
                await PrintService.PrintAsync(url, id);
            }
            else
            {
                await PrintService.OpenPrintFormAsync(url, id, PrintConfig);
            }
        }

        private readonly MudBlazor.Converter<bool, bool?> _oppositeBoolConverter = new()
        {
            SetFunc = value => !value,
            GetFunc = value => !value ?? true,
        };

        [GeneratedRegex("[^a-zA-Z]")]
        private static partial Regex ExportTitleRegex();
        [GeneratedRegex("\\$skip=[0-9]+&?|\\$top=[0-9]+&?")]
        private static partial Regex ExportUrlRegex();

        public void Dispose()
        {
            ShiftBlazorEvents.OnModalClosed -= ShiftBlazorEvents_OnModalClosed;
            IShortcutComponent.Remove(Id);
            GC.SuppressFinalize(this);
        }
    }

    
}