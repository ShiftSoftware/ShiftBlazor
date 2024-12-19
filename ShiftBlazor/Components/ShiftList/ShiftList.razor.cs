using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    [CascadingTypeParameter(nameof(T))]
    public partial class ShiftList<T> : IODataComponent, IShortcutComponent, ISortableComponent, IFilterableComponent, IShiftList where T : ShiftEntityDTOBase, new()
    {
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] HttpClient HttpClient { get; set; } = default!;
        [Inject] ShiftModal ShiftModal { get; set; } = default!;
        [Inject] ShiftBlazorLocalizer Loc  { get; set; } = default!;
        [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] MessageService MessageService { get; set; } = default!;
        [Inject] NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        protected MudDialogInstance? MudDialog { get; set; }

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
        public EventCallback<List<T>> OnFetch { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

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


        public Uri? CurrentUri { get; set; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();
        public bool IsEmbed { get; private set; } = false;
        [Obsolete]
        public HashSet<T> SelectedItems => SelectState.Items.ToHashSet();
        [Obsolete]
        public bool IsAllSelected => SelectState.All;
        public readonly SelectState<T> SelectState = new();

        internal event EventHandler<KeyValuePair<Guid, List<T>>>? _OnBeforeDataBound;
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
        private ODataFilterGenerator Filters = new ODataFilterGenerator(true);
        private string PreviousFilters = string.Empty;
        private bool ReadyToRender = false;
        private bool IsModalOpen = false;
        private bool IsGridEditorOpen = false;
        private bool IsDeleteColumnHidden = true;
        private string GridEditorHeight => string.IsNullOrWhiteSpace(Height) ? "350px" : $"calc({Height} - 50px)";

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
                string? url = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
                
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
                Filters.Add(filter);
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
        public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null)
        {
            Filters.Add(field, op, value, id);
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
                    await ViewAddItem();
                    break;
                case KeyboardKeys.KeyE:
                    await ExportList();
                    break;
                case KeyboardKeys.KeyC:
                    OpenGridEditor();
                    break;
            }
        }

        private async Task<GridData<T>> ServerReload(GridState<T> state)
        {
            var builder = QueryBuilder;
            ErrorMessage = null;

            // Save current PageSize as user preference 
            if (state.PageSize != SelectedPageSize)
            {
                SettingManager.SetListPageSize(state.PageSize);
            }

            // Convert MudBlazor's SortDefinitions to OData query
            if (state.SortDefinitions.Count > 0)
            {
                var sortList = state.SortDefinitions.ToODataFilter();
                builder = builder.AddQueryOption("$orderby", string.Join(',', sortList));
            }

            // Remove multiple empty filters but keep the last added empty filter
            var emptyFields = state.FilterDefinitions.Where(x => x.Value == null && x.Operator != FilterOperator.String.Empty && x.Operator != FilterOperator.String.NotEmpty);
            for (var i = 0; i < emptyFields.Count() - 1; i++)
            {
                DataGrid!.FilterDefinitions.Remove(emptyFields.ElementAt(i));
            }

            try
            {
                // Convert MudBlazor's FilterDefinitions to OData query
                var userFilters = state.FilterDefinitions.ToODataFilter().Select(x => string.Join(" or ", x));

                var filterList = new List<string>();
                filterList.AddRange(userFilters);

                if (Filters.Count > 0)
                    filterList.Add(Filters.ToString());

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
                ErrorMessage = $"An error has occured";
                MessageService.Error(Loc["ShiftListFilterParseError"], e.Message, e!.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }

            var builderQueryable = builder.AsQueryable();

            // apply pagination values
            if (!EnableVirtualization)
            {
                builderQueryable = builderQueryable
                    .Skip(state.Page * state.PageSize)
                    .Take(state.PageSize);
            }

            var url = builderQueryable.ToString();
            GridData<T> gridData = new();
            HttpResponseMessage? res = default;

            try
            {
                CurrentUri = new Uri(url!);
                res = await HttpClient.GetAsync(url);

                if (!res!.IsSuccessStatusCode)
                {
                    ErrorMessage = Loc["DataReadStatusError", (int)res!.StatusCode];
                    ReadyToRender = true;
                    return gridData;
                }

                var content = await res.Content.ReadFromJsonAsync<ODataDTO<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });

                if (content == null || content.Count == null)
                {
                    ErrorMessage = Loc["DataReadEmptyError"];
                    ReadyToRender = true;
                    return gridData;
                }

                gridData = new GridData<T>
                {
                    Items = content.Value.ToList(),
                    TotalItems = (int)content.Count.Value,
                };

                SelectState.Total = gridData.TotalItems;

                await OnFetch.InvokeAsync(content.Value);
                ShiftBlazorEvents.TriggerOnBeforeGridDataBound(new KeyValuePair<Guid, List<object>>(Id, content.Value.ToList<object>()));
                _OnBeforeDataBound?.Invoke(this, new KeyValuePair<Guid, List<T>>(Id, content.Value));
            }
            catch (JsonException e)
            {
                var body = await res!.Content.ReadAsStringAsync();
                ErrorMessage = Loc["DataParseError"];
                MessageService.Error(Loc["DataReadError"], e.Message, body, buttonText: Loc["DropdownViewButtonText"]);
            }
            catch (Exception e)
            {
                ErrorMessage = Loc["DataReadError"];
                MessageService.Error(Loc["DataReadError"], e.Message, e!.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            }

            ReadyToRender = true;

            return gridData;
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

                if (idProp == null || textProp == null || foreignData == null || columnProperty == null)
                    return;

                foreach (var row in items)
                {
                    var id = columnProperty.GetValue(row);

                    var test = foreignData.FirstOrDefault(x => idProp.GetValue(x)?.ToString() == id?.ToString());
                    if (test != null)
                    {
                        lock (lockObject)
                        {
                            columnProperty.SetValue(row, textProp.GetValue(test));
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
                if (res?.Value == null || !res.Value.Any())
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
                        .Where(x => !x.Hidden)
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

        private readonly MudBlazor.Converter<bool, bool?> _oppositeBoolConverter = new()
        {
            SetFunc = value => !value,
            GetFunc = value => !value ?? true,
        };

        [GeneratedRegex("[^a-zA-Z]")]
        private static partial Regex ExportTitleRegex();
        [GeneratedRegex("\\$skip=[0-9]+&?|\\$top=[0-9]+&?")]
        private static partial Regex ExportUrlRegex();

        void IDisposable.Dispose()
        {
            ShiftBlazorEvents.OnModalClosed -= ShiftBlazorEvents_OnModalClosed;
            IShortcutComponent.Remove(Id);
        }
    }
}