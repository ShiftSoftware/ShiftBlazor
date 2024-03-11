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
    public partial class ShiftList<T> : IODataComponent, IShortcutComponent, ISortableComponent, IShiftList where T : ShiftEntityDTOBase, new()
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
        ///     To check whether this list is currently embeded inside a form component.
        /// </summary>
        [CascadingParameter(Name = FormHelper.ParentReadOnlyName)]
        public bool? ParentReadOnly { get; set; }

        [CascadingParameter(Name = FormHelper.ParentDisabledName)]
        public bool? ParentDisabled { get; set; }

        /// <summary>
        ///     The current fetched items, this will be fetched from the OData API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public List<T>? Values { get; set; }

        /// <summary>
        ///     An event triggered when the state of Values has changed.
        /// </summary>
        [Parameter]
        public EventCallback<List<T>> ValuesChanged { get; set; }

        /// <summary>
        ///     OData EntitySetName.
        /// </summary>
        [Parameter]
        public string? EntitySet { get; set; }

        [Parameter]
        public string? BaseUrl { get; set; }

        [Parameter]
        public string? BaseUrlKey { get; set; }

        /// <summary>
        ///     The type of the component to open when clicking on Add or the Action button.
        ///     If empty, Add and Action button column will be hidden.
        /// </summary>
        [Parameter]
        public Type? ComponentType { get; set; }

        /// <summary>
        ///     To pass additional parameters to the ShiftFormContainer componenet.
        /// </summary>
        [Parameter]
        public Dictionary<string, string>? AddDialogParameters { get; set; }

        /// <summary>
        ///     Enable select
        /// </summary>
        [Parameter]
        public bool EnableSelection { get; set; }

        /// <summary>
        ///     Enable Virtualization and disable Paging.
        /// </summary>
        [Parameter]
        public bool EnableVirtualization { get; set; }

        /// <summary>
        ///     To set the list's fixed height.
        /// </summary>
        [Parameter]
        public string Height { get; set; } = string.Empty;

        /// <summary>
        ///     The title to render on the form header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

        /// <summary>
        ///     If true, the toolbar in the header will not be rendered.
        /// </summary>
        [Parameter]
        public bool DisableHeaderToolbar { get; set; }

        [Parameter]
        public bool DisableActionColumn { get; set; }

        [Parameter]
        public string? NavColor { get; set; }

        [Parameter]
        public bool NavIconFlatColor { get; set; }

        /// <summary>
        /// The icon displayed before the Form Title, in a string in SVG format.
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = @Icons.Material.Filled.List;

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
        ///     Used to add custom elements to the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the controls section of the header toolbar.
        ///     This section is only visible when the form is opened in a dialog.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarControlsTemplate { get; set; }

        [Parameter]
        public bool DisableDeleteFilter { get; set; }

        [Parameter]
        public bool DisableColumnChooser { get; set; }

        /// <summary>
        ///     Disable the add item button to open a form.
        /// </summary>
        [Parameter]
        public bool DisableAdd { get; set; }

        [Parameter]
        public bool Dense { get; set; }

        [Parameter]
        public EventCallback<DataGridRowClickEventArgs<T>> OnRowClick { get; set; }

        [Parameter]
        public EventCallback<object?> OnFormClosed { get; set; }

        [Parameter]
        public EventCallback OnLoad { get; set; }

        [Parameter]
        public EventCallback<HashSet<T>> OnSelectedItemsChanged { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool ShowIDColumn { get; set; }

        /// <summary>
        ///     The number of items to be displayed per page.
        /// </summary>
        [Parameter]
        public int? PageSize { get; set; }

        [Parameter]
        public bool EnableExport { get; set; }

        [Parameter]
        public bool DisableStickyHeader { get; set; }

        [Parameter]
        public bool DisablePagination { get; set; }

        [Parameter]
        public bool DisableSorting { get; set; }

        [Parameter]
        public bool DisableMultiSorting { get; set; }

        [Parameter]
        public bool DisableFilters { get; set; }

        /// <summary>
        ///     Used to override any element in the Action column.
        /// </summary>
        [Parameter]
        public RenderFragment<CellContext<T>>? ActionsTemplate { get; set; }

        [Parameter]
        public Action<ODataFilter>? Filter { get; set; }

        [Parameter]
        public string? FilterString { get; set; }

        [Parameter]
        public bool Outlined { get; set; }

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
        internal string? DefaultFilter;

        internal Func<GridState<T>, Task<GridData<T>>>? ServerData = default;

        private MudDataGrid<T>? _DataGrid;
        public MudDataGrid<T>? DataGrid
        {
            get => _DataGrid;
            set
            {
                _DataGrid = value;
                OnLoadHandler();
            }
        }

        protected override void OnInitialized()
        {
            IsEmbed = ParentDisabled != null || ParentReadOnly != null;

            if (MudDialog != null && !IsEmbed)
            {
                IShortcutComponent.Register(this);
            }

            if (Values == null && EntitySet == null)
            {
                throw new ArgumentNullException($"{nameof(Values)} and {nameof(EntitySet)} are null");
            }

            TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();

            if (EntitySet != null)
            {
                string? url = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
                
                QueryBuilder = OData
                    .CreateNewQuery<T>(EntitySet, url)
                    .IncludeCount();
            }

            if (Filter != null)
            {
                var filter = new ODataFilter();
                Filter.Invoke(filter);
                DefaultFilter = filter.ToString();
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

        protected override void OnParametersSet()
        {
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

        public ValueTask HandleShortcut(KeyboardKeys key)
        {
            switch (key)
            {
                case KeyboardKeys.Escape:
                    CloseDialog();
                    break;
            }

            return new ValueTask();
        }

        private void OnLoadHandler()
        {
            OnLoad.InvokeAsync();
        }

        private void HideDisabledColumns()
        {
            var columns = DataGrid?.RenderedColumns.Where(x => x.Hideable == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

            var columnStates = SettingManager.GetHiddenColumns(GetListIdentifier()).ToList();

            foreach (var item in columnStates)
            {
                var column = columns.FirstOrDefault(x => x.Title == item.Title);
                _ = item.Visible == true
                    ? column?.ShowAsync()
                    : column?.HideAsync();
            }

            foreach (var item in columns)
            {
                #pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                item.HiddenChanged = new EventCallback<bool>(this, ColumnStateChanged);
                #pragma warning restore BL0005
            }
        }

        public async Task ViewAddItem(object? key = null)
        {
            if (ComponentType != null)
            {
                var result = await OpenDialog(ComponentType, key, ModalOpenMode.Popup, this.AddDialogParameters);
                await OnFormClosed.InvokeAsync(result?.Data);
            }
        }

        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            var result = await ShiftModal.Open(ComponentType, key, openMode, parameters);
            if (result != null && result.Canceled != true)
            {
                await DataGrid!.ReloadServerData();
            }
            return result;
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

        private void SaveColumnState()
        {
            var columns = DataGrid?.RenderedColumns.Where(x => x.Hideable == true);
            if (columns == null || !columns.Any())
            {
                return;
            }

            var columnStates = columns.Select(x => new ColumnState
            {
                Title = x.Title,
                Visible = !x.Hidden
            }).ToList();

            SettingManager.SetHiddenColumns(GetListIdentifier(), columnStates);
        }

        private void ColumnStateChanged(bool hidden)
        {
            SaveColumnState();
        }

        internal async Task RerenderDataGrid()
        {
            await InvokeAsync(StateHasChanged);
        }

        public void GridStateHasChanged()
        {
            StateHasChanged();
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
            var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url);

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
                            //  Get the column's Property parameter
                            var ColumnExpression = column.GetType().GetProperty("Property")?.GetValue(column);
                            if (ColumnExpression is LambdaExpression lambdaExpression)
                            {
                                // Compile and invoke the method so we can replicate the result shown on the DataGrid
                                var compiled = lambdaExpression.Compile();
                                try
                                {
                                    object? result = compiled.DynamicInvoke(item);
                                    csvWriter.WriteField(result);
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
            using var streamRef = new DotNetStreamReference(stream: stream);
            await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
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

        public async Task SetSortAsync(string field, SortDirection sortDirection)
        {
            if (DataGrid != null)
            {
                await DataGrid.SetSortAsync(field, sortDirection, null);
            }
        }

        public void SetSort(string field, SortDirection sortDirection)
        {
            DataGrid?.SortDefinitions.Add(field, new SortDefinition<T>(field, sortDirection == SortDirection.Descending, 0, null));
            InvokeAsync(StateHasChanged);
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

                if (!string.IsNullOrWhiteSpace(FilterString))
                    filterList.Add(FilterString);

                if (!string.IsNullOrWhiteSpace(DefaultFilter)) 
                    filterList.Add(DefaultFilter);

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
                    return gridData;
                }

                var content = await res.Content.ReadFromJsonAsync<ODataDTO<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new LocalDateTimeOffsetJsonConverter() }
                });
                if (content == null || content.Count == null)
                {
                    ErrorMessage = $"Could not read server data (empty content)";
                    return gridData;
                }

                gridData = new GridData<T>
                {
                    Items = content.Value.ToList(),
                    TotalItems = (int)content.Count.Value,
                };

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

            return gridData;
        }

        [GeneratedRegex("[^a-zA-Z]")]
        private static partial Regex ExportTitleRegex();
        [GeneratedRegex("\\$skip=[0-9]+&?|\\$top=[0-9]+&?")]
        private static partial Regex ExportUrlRegex();

        void IDisposable.Dispose()
        {
            IShortcutComponent.Remove(Id);
        }
    }
}