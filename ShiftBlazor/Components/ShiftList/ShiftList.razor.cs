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
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    [CascadingTypeParameter(nameof(T))]
    public partial class ShiftList<T> : IODataRequestComponent<T>, IShortcutComponent, ISortableComponent, IFilterableComponent, IShiftList where T : ShiftEntityDTOBase, new()
    {
        [Inject] ISnackbar Snackbar { get; set; } = default!;
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
        [Obsolete]
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
        [Obsolete]
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
        public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();
        public bool IsEmbed { get; private set; } = false;
        [Obsolete]
        public HashSet<T> SelectedItems => SelectState.Items.ToHashSet();
        [Obsolete]
        public bool IsAllSelected => SelectState.All;
        public readonly SelectState<T> SelectState = new();

        internal Size IconSize = Size.Medium;
        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;
        internal bool RenderAddButton = false;
        internal int SelectedPageSize;
        internal int[] PageSizes = new int[] { 5, 10, 50, 100, 250, 500 };
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
        private DotNetObjectReference<ShiftList<T>>? dotNetRef;
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
            dotNetRef = DotNetObjectReference.Create(this);

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

            _ = JsRuntime.InvokeVoidAsync("fixStickyColumn", $"Grid-{Id}");
        }

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

            if (Filters.ToString() != PreviousFilters)
            {
                PreviousFilters = Filters.ToString();
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
            // Convert MudBlazor's SortDefinitions to OData query
            if (state.SortDefinitions.Count > 0)
            {
                var sortList = state.SortDefinitions.ToODataFilter();
                builder = builder.AddQueryOption("$orderby", string.Join(',', sortList));
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

                if (filterList.Any())
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
                ErrorMessage = $"An error has occurred";
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

        private void ColumnHiddenStateChanged(string name, bool hidden)
        {
            var col = DataGrid?.RenderedColumns.FirstOrDefault(x => (x.Hideable ?? DataGrid?.Hideable) == true && x.Title == name);
            col.Hidden = hidden;
            SaveColumnState();
        }

        private void HideDisabledColumns(List<ColumnState> columnStates)
        {
            var columns = DataGrid?.RenderedColumns.Where(x => (x.Hideable ?? DataGrid?.Hideable) == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
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
                    item.HiddenChanged = new EventCallback<bool>(this, delegate (bool value) { ColumnHiddenStateChanged(item.Title, value); });
                }
            }
            catch (Exception e)
            {
                MessageService.Error(Loc["HideDisabledColumnError"], e.Message, e.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }
#pragma warning restore BL0005
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

        private void ToggleSticky(Column<T> column, bool sticky)
        {
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
            column.StickyLeft = sticky;
            column.StickyRight = sticky;
            SaveColumnState();
#pragma warning restore BL0005
        }

        private Task ColumnOrderUpdated(MudItemDropInfo<Column<T>> dropItem)
        {
            if (DataGrid != null)
            {
                DataGrid.RenderedColumns.Remove(dropItem.Item);
                DataGrid.RenderedColumns.Insert(dropItem.IndexInZone, dropItem.Item);

                DropContainerHasChanged();

                SaveColumnState();
            }

            return Task.CompletedTask;
        }

        private void SaveColumnState()
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
                _ = JsRuntime.InvokeVoidAsync("fixStickyColumn", $"Grid-{Id}");
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

        #region Export  

        public static Dictionary<string, string>? GetEnumMap(Type? enumType)
        {
            if (enumType == null || !enumType.IsEnum)
                return null;

            var result = new Dictionary<string, string>();

            foreach (Enum val in Enum.GetValues(enumType))
            {
                var description = val.Describe(); //description ?? name;

                // Add both string and int representations of the enum value
                // This is useful for cases where the enum value is used as a string in some contexts and as an integer in others.

                //The Name property of the enum value is used as the key for the string representation.
                result[val.ToString()] = description;
                // ["SomeEnum"] = "Some Description"


                //The integer value of the enum is converted to a string and used as the key for the integer representation.
                result[Convert.ToInt32(val).ToString()] = description;
                // ["1"] = "Some Description"
            }

            return result;
        }

        internal async Task ExportList()
        {
            if (ExportIsInProgress)
                return;

            try
            {
                this.ExportIsInProgress = true;

                var payload = BuildExportPayload();

                Snackbar.RemoveByKey($"export_table_{payload.Name}");
                MessageService.Show($"📦 '{payload.Name}' export started", severity: Severity.Info, modalColor: Color.Info, icon: Icons.Material.TwoTone.FileCopy, key: $"export_table_{payload.Name}");

                await JsRuntime.InvokeVoidAsync("tableExport", payload, dotNetRef);
            }
            catch (Exception e)
            {
                MessageService.Show($"❌ Export failed: {e.Message}", Severity.Error);
                this.ExportIsInProgress = false;
            }
        }

        internal ExportPayload BuildExportPayload()
        {
            var name = Title != null && ExportTitleRegex().IsMatch(Title)
                ? Title
                : EntitySet ?? typeof(T).Name;

            name = ExportTitleRegex().Replace(name, "");
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(name) ? $"file_{date}.csv" : $"{name}.csv";

            var urlValue = Values == null && CurrentUri != null
                ? CleanExportUrlRegex().Replace(CurrentUri.AbsoluteUri, "")
                : string.Empty;
            var values = Values ?? [];

            var foreignColumns = DataGrid!
                    .RenderedColumns
                    .Where(x => x is IForeignColumn)
                    .Select(x =>
                    {
                        var foreignColumn = x as IForeignColumn;

                        var fullName = foreignColumn!.GetType().GetGenericArguments().Last().FullName;

                        var parts = fullName!.Split('.');
                        var tableName = parts.Length >= 2 ? parts[^2] : fullName;

                        foreignColumn.ForeignEntityField = tableName;

                        return foreignColumn;
                    });

            var columns = DataGrid!
                .RenderedColumns
                .Where(x => x.GetType().GetProperty(nameof(PropertyColumn<object, object>.Property)) != null)
                .Select(x =>
                {
                    var key = x.PropertyName;
                    if (x.GetType().GetProperty(nameof(PropertyColumn<object, object>.Property))?.GetValue(x) is LambdaExpression propertyValue)
                    {
                        key = Misc.GetPropertyPath(propertyValue).Split(".").First();
                    }

                    var property = typeof(T).GetProperty(key) ?? null;

                    var format = property?.GetCustomAttribute<ShiftListNumberFormatterExportAttribute>()?.Format;
                    var customColumn = property?.GetCustomAttribute<ShiftListCustomColumnExportAttribute>()?.ToList();

                    var enumValues = property == null
                        ? null
                        : GetEnumMap(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);

                    return new ExportColumn(
                        key,
                        format,
                        enumValues,
                        customColumn,
                        x.Title,
                        x.Hidden
                    );
                });

            var setting = new ExportSetting(
                fileName,
                SettingManager.GetLanguage().RTL,
                SettingManager.GetCulture().TwoLetterISOLanguageName,
                SettingManager.GetDateFormat(),
                SettingManager.GetTimeFormat()
                );

            return new(
                name,
                urlValue,
                values,
                columns,
                foreignColumns,
                setting
            );
        }

        public sealed record ExportPayload(string Name, string UrlValue, IReadOnlyList<T>? Values, IEnumerable<ExportColumn> Columns, IEnumerable<IForeignColumn> ForeignColumns, ExportSetting Setting);
        public sealed record ExportColumn(string Key, string? Format, IReadOnlyDictionary<string, string>? EnumValues, IReadOnlyList<object>? CustomColumn, string Title, bool Hidden);
        public sealed record ExportSetting(string FileName, bool IsRTL, string Language, string DateFormat, string TimeFormat);

        [JSInvokable]
        public void OnExportProcessing(string name)
        {
            Snackbar.RemoveByKey($"export_table_{name}");
            MessageService.Show($"'{name}' export is still processing... This might take a while.", severity: Severity.Warning, modalColor: Color.Warning, icon: Icons.Material.Filled.MoreTime, key: $"export_table_{name}");
        }

        [JSInvokable]
        public void OnExportProcessed(bool isSuccess, string message, string name)
        {
            this.ExportIsInProgress = false;

            Snackbar.RemoveByKey($"export_table_{name}");

            if (isSuccess)
                MessageService.Show($"'{name}' export completed successfully!", severity: Severity.Success, modalColor: Color.Success, icon: Icons.Material.TwoTone.CheckCircle, key: $"export_table_{name}");
            else
                MessageService.Show(Loc["Export failed"], Loc["Export failed"], message, severity: Severity.Error, buttonText: Loc["DropdownViewButtonText"], modalColor: Color.Error, key: $"export_table_{name}");
            
            StateHasChanged();
        }

        [GeneratedRegex("[^a-zA-Z]")]
        private static partial Regex ExportTitleRegex();
        [GeneratedRegex("\\$skip=[0-9]+&?|\\$top=[0-9]+&?")]
        private static partial Regex CleanExportUrlRegex();

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

        public void Dispose()
        {
            dotNetRef?.Dispose();
            ShiftBlazorEvents.OnModalClosed -= ShiftBlazorEvents_OnModalClosed;
            IShortcutComponent.Remove(Id);
        }
    }

    
}