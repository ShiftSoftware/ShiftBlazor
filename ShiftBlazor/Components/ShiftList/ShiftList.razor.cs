using CsvHelper;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    [CascadingTypeParameter(nameof(T))]
    public partial class ShiftList<T> where T : ShiftEntityDTOBase, new()
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
        public Expression<Func<T, bool>>? Where { get; set; }

        [Parameter]
        public bool Outlined { get; set; }


        public bool IsAllSelected = false;
        public HashSet<T> SelectedItems { get; set; } = new();
        public Uri? CurrentUri { get; set; }


        internal event EventHandler<KeyValuePair<Guid, List<T>>>? _OnBeforeDataBound;
        internal bool IsEmbed => ParentDisabled != null || ParentReadOnly != null;
        internal Size IconSize => Dense ? Size.Medium : Size.Large;
        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;
        internal bool RenderAddButton => !(DisableAdd || ComponentType == null || (TypeAuthAction != null && !TypeAuthService.Can(TypeAuthAction, TypeAuth.Core.Access.Write)));
        internal int SelectedPageSize;
        internal int[] PageSizes = new int[] { 5, 10, 50, 100, 250, 500 };
        internal Guid DataGridId = Guid.NewGuid();
        internal bool? deleteFilter = false;
        internal string? ErrorMessage;
        private ITypeAuthService? TypeAuthService;
        private string ToolbarStyle => $"{ColorHelperClass.GetToolbarStyles(NavColor, NavIconFlatColor)}border: 0;";

        internal SortMode SortMode => DisableSorting
                                        ? SortMode.None
                                        : DisableMultiSorting
                                            ? SortMode.Single
                                            : SortMode.Multiple;

        internal Func<GridState<T>, Task<GridData<T>>>? ServerData => Values == null
            ? new Func<GridState<T>, Task<GridData<T>>>(ServerReload)
            : default;

        private MudDataGrid<T>? _DataGrid;
        public MudDataGrid<T>? DataGrid
        {
            get
            {
                return _DataGrid;
            }
            set
            {
                _DataGrid = value;
                OnLoadHandler();
            }
        }

        protected override void OnInitialized()
        {
            if (Values == null && EntitySet == null)
            {
                throw new ArgumentNullException($"{nameof(Values)} and {nameof(EntitySet)} are null");
            }

            TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();

            if (EntitySet != null)
            {
                QueryBuilder = OData
                    .CreateQuery<T>(EntitySet)
                    .IncludeCount();
            }

            if (PageSize != null && !PageSizes.Any(x => x == PageSize))
            {
                PageSizes = PageSizes.Append(PageSize.Value).Order().ToArray();
            }

            SelectedPageSize = SettingManager.Settings.ListPageSize ?? PageSize ?? DefaultAppSetting.ListPageSize;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender && !DisableColumnChooser)
            {
                HideDisabledColumns();
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

        private void OnLoadHandler()
        {
            OnLoad.InvokeAsync();
        }

        private void HideDisabledColumns()
        {
            var columns = DataGrid?.RenderedColumns.Where(x => x.Hideable == true);
            if (columns == null || columns.Count() == 0)
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
            deleteFilter = delete;
            _ = DataGrid?.ReloadServerData();
        }

        private void CloseDialog()
        {
            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
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
            if (columns == null || columns.Count() == 0)
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

        private async Task<Stream> GetStream(string url)
        {
            var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url);
            var stream = new MemoryStream();

            if (res != null && res.Value.Count > 0)
            {
                using (var streamWriter = new StreamWriter(stream, leaveOpen: true))
                {
                    var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                    await csvWriter.WriteRecordsAsync(res.Value);
                }

                stream.Seek(0, SeekOrigin.Begin);
            }

            return stream;
        }

        internal async Task ExportList()
        {
            if (CurrentUri == null) return;

            var name = Title != null && Regex.IsMatch(Title, "[^a-zA-Z]") ? Title : EntitySet;
            name = Regex.Replace(name!, "[^a-zA-Z]", "");
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(name) ? $"file_{date}.csv" : $"{name}_{date}.csv";

            var url = Regex.Replace(CurrentUri.AbsoluteUri, "\\$skip=[0-9]+&?|\\$top=[0-9]+&?", "");
            var fileStream = await GetStream(url);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
            
        internal void SelectedItemsChangedHandler(HashSet<T> items)
        {
            SelectedItems = items;
            OnSelectedItemsChanged.InvokeAsync(items);
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

            // Convert MudBlazor's FilterDefinitions to OData query
            var filterList = state.FilterDefinitions.ToODataFilter();

            if (filterList.Count() > 0)
            {
                builder = builder.AddQueryOption("$filter", string.Join(" and ", filterList));
            }

            var builderQueryable = builder.AsQueryable();

            try
            {
                // apply custom filters
                if (Where != null)
                {
                    builderQueryable = builderQueryable.Where(Where);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = $"An error has occured";
                MessageService.Error("Could not custom parse filter", e.Message, e!.ToString());
            }

            // apply delete filters
            if (deleteFilter == true)
            {
                builderQueryable = builderQueryable.Where(x => x.IsDeleted == true);
            }
            else if (deleteFilter == null)
            {
                builderQueryable = builderQueryable.Where(x => x.IsDeleted == true || x.IsDeleted == false);
            }

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

                var content = await res.Content.ReadFromJsonAsync<ODataDTO<T>>();
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

                if (_OnBeforeDataBound != null)
                {
                    _OnBeforeDataBound(this, new KeyValuePair<Guid, List<T>>(DataGridId, content.Value.ToList()));
                }
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
    }
}