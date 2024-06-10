using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Linq.Expressions;
using System.Net.Http.Json;
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
        [Inject] IStringLocalizer<Resources.Components.ShiftList> Loc { get; set; } = default!;
        [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] MessageService MessageService { get; set; } = default!;

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
        public Dictionary<string, string>? AddDialogParameters { get; set; }

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
        public bool DisableActionColumn { get; set; }

        /// <summary>
        /// When true, the Delete Filter will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableDeleteFilter { get; set; }

        /// <summary>
        /// When true, the Column Chooser will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableColumnChooser { get; set; }

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
        public EventCallback<DataGridRowClickEventArgs<T>> OnRowClick { get; set; }

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
        public EventCallback<HashSet<T>> OnSelectedItemsChanged { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Whether to render or not render 'Entity ID' column
        /// </summary>
        [Parameter]
        public bool ShowIDColumn { get; set; }

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

        public HashSet<T> SelectedItems => DataGrid?.SelectedItems ?? new HashSet<T>();
        public Uri? CurrentUri { get; set; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();
        public bool IsEmbed { get; private set; } = false;
        public bool IsAllSelected { get; private set; } = false;


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
        private int TotalItemCount = 0;

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
                TotalItemCount = Values.Count;
            }

            SelectedPageSize = SettingManager.Settings.ListPageSize ?? PageSize ?? DefaultAppSetting.ListPageSize;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                if (!DisableColumnChooser)
                {
                    HideDisabledColumns();
                }
            }
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
                DataGrid?.ReloadServerData();
            }

            if (DataGrid == null)
            {
                return;
            }

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
        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
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
                    OpenColumnChooser();
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
                    builder = builder.AddQueryOption("$filter", $"({string.Join(") and (", filterList)})");
                }
            }
            catch (Exception e)
            {
                ErrorMessage = $"An error has occured";
                MessageService.Error("Could not parse filter", e.Message, e!.ToString());
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
                    ErrorMessage = $"Could not read server data ({(int)res!.StatusCode})";
                    ReadyToRender = true;
                    return gridData;
                }

                var content = await res.Content.ReadFromJsonAsync<ODataDTO<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });

                if (content == null || content.Count == null)
                {
                    ErrorMessage = $"Could not read server data (empty content)";
                    ReadyToRender = true;
                    return gridData;
                }

                gridData = new GridData<T>
                {
                    Items = content.Value.ToList(),
                    TotalItems = (int)content.Count.Value,
                };

                if (IsAllSelected)
                {
                    DataGrid.SelectedItems = content.Value.ToHashSet();
                }
                ShiftBlazorEvents.TriggerOnBeforeGridDataBound(new KeyValuePair<Guid, List<object>>(Id, content.Value.ToList<object>()));

                _OnBeforeDataBound?.Invoke(this, new KeyValuePair<Guid, List<T>>(Id, content.Value.ToList()));
            }
            catch (JsonException e)
            {
                var body = await res!.Content.ReadAsStringAsync();
                ErrorMessage = $"Could not read server data (parse error)";
                MessageService.Error("Could not read server data", e.Message, body);
            }
            catch (Exception e)
            {
                ErrorMessage = $"Could not read server data";
                MessageService.Error("Could not read server data", e.Message, e!.ToString());
            }

            TotalItemCount = gridData.TotalItems;
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

        private void HideDisabledColumns()
        {
            var columns = DataGrid?.RenderedColumns.Where(x => (x.Hideable ?? DataGrid?.Hideable) == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

            try
            {
                var columnStates = SettingManager.GetHiddenColumns(GetListIdentifier()).ToList();

                foreach (var item in columnStates)
                {
                    var column = columns.FirstOrDefault(x => x.Title == item.Title);
                    if (column != null)
                    {
                        column.Hidden = !item.Visible;
                        _ = item.Visible == true
                            ? column?.ShowAsync()
                            : column?.HideAsync();
                    }
                }

                foreach (var item in columns)
                {
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                    item.HiddenChanged = new EventCallback<bool>(this, delegate (bool value) { ColumnStateChanged(item.Title, value); });
#pragma warning restore BL0005
                }
            }
            catch (Exception e)
            {
                MessageService.Error("Could not get or set column states", e.Message, e.ToString());
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
                await OnRowClick.InvokeAsync(args);
            }
        }

        private void OpenColumnChooser()
        {
            DataGrid?.ShowColumnsPanel();
        }
        private string GetListIdentifier()
        {
            return $"{EntitySet}_{typeof(T).Name}";
        }

        private void ColumnStateChanged(string name, bool hidden)
        {
            var col = DataGrid?.RenderedColumns.FirstOrDefault(x => (x.Hideable ?? DataGrid?.Hideable) == true && x.Title == name);
            col.Hidden = hidden;
            SaveColumnState();
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
            }).ToList();

            SettingManager.SetHiddenColumns(GetListIdentifier(), columnStates);
        }

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

            foreach (var column in foreignColumns)
            {
                if (column == null) continue;

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
                    continue;

                foreach (var row in items)
                {
                    var id = columnProperty.GetValue(row);

                    var test = foreignData.FirstOrDefault(x => idProp.GetValue(x)?.ToString() == id?.ToString());
                    if (test != null)
                    {
                        columnProperty.SetValue(row, textProp.GetValue(test));
                    }
                }
            }
        }

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
                MessageService.Error("Error processing foreign columns", "Error processing foreign columns", e.ToString());
            }

            return GetStream(res?.Value);
        }

        private Stream GetStream(List<T>? items)
        {
            var stream = new MemoryStream();

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);

            if (items != null && items.Count > 0)
            {
                using (var streamWriter = new StreamWriter(stream, leaveOpen: true))
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
        }

        internal void SelectedItemsChangedHandler(HashSet<T> items)
        {
            OnSelectedItemsChanged.InvokeAsync(items);
        }

        internal async Task SelectRow(T item)
        {
            var removedItems = DataGrid?.SelectedItems.RemoveWhere(x => x.ID == item.ID);

            if (removedItems == 0 && !IsAllSelected)
            {
                DataGrid?.SelectedItems.Add(item);
            }

            IsAllSelected = false;
            await OnSelectedItemsChanged.InvokeAsync(SelectedItems);
        }

        internal async Task SelectAll(HeaderContext<T> context, bool selectAll)
        {
            IsAllSelected = selectAll;
            await context.Actions.SetSelectAllAsync(selectAll);
        }

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