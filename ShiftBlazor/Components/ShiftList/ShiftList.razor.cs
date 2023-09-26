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
        public EventCallback<T> ValuesChanged { get; set; }

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

        /// <summary>
        ///     Enable Print button.
        /// </summary>
        [Parameter]
        public bool EnablePrint { get; set; }

        [Parameter]
        public bool Dense { get; set; }

        [Parameter]
        public EventCallback<DataGridRowClickEventArgs<T>> OnRowClick { get; set; }

        [Parameter]
        public EventCallback<object?> OnFormClosed { get; set; }

        [Parameter]
        public EventCallback OnLoad { get; set; }

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


        public bool IsAllSelected = false;

        internal event EventHandler<KeyValuePair<Guid, List<T>>>? _OnBeforeDataBound;
        internal bool IsEmbed => ParentDisabled != null || ParentReadOnly != null;
        internal Size IconSize => Dense ? Size.Medium : Size.Large;
        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;
        internal bool RenderAddButton => !(DisableAdd || ComponentType == null || (TypeAuthAction != null && !TypeAuthService.Can(TypeAuthAction, TypeAuth.Core.Access.Write)));
        internal int SelectedPageSize;
        internal int[] PageSizes = new int[] { 5, 10, 50, 100, 250, 500 };
        internal Guid DataGridId = Guid.NewGuid();
        internal bool? deleteFilter = false;
        private ITypeAuthService? TypeAuthService;

        internal SortMode SortMode => DisableSorting
                                        ? SortMode.None
                                        : DisableMultiSorting
                                            ? SortMode.Single
                                            : SortMode.Multiple;

        internal Func<GridState<T>, Task<GridData<T>>>? ServerData => Values == null
            ? new Func<GridState<T>, Task<GridData<T>>>(ServerReload)
            : default;

        private MudDataGrid<T>? _DataGrid;
        internal MudDataGrid<T>? DataGrid
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
                    item.HiddenChanged = new EventCallback<bool>(this, ColumnStateChanged);
                }
            }
        }

        private void OnLoadHandler()
        {
            OnLoad.InvokeAsync();
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

        public async Task PrintList()
        {
            throw new NotImplementedException();
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

        private async Task<Stream> GetFileStream()
        {
            var url = QueryBuilder.Where(x => x.IsDeleted == true || x.IsDeleted == false);
            var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url.ToString());
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
            var name = Title != null && Regex.IsMatch(Title, "[^a-zA-Z]") ? Title : EntitySet;
            name = Regex.Replace(name!, "[^a-zA-Z]", "");
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(name) ? $"file_{date}.csv" : $"{name}_{date}.csv";

            var fileStream = await GetFileStream();
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }

        private async Task<GridData<T>> ServerReload(GridState<T> state)
        {
            var builder = QueryBuilder;

            // Save current PageSize as user preference 
            if (state.PageSize != SelectedPageSize)
            {
                SettingManager.SetListPageSize(state.PageSize);
            }

            #region add filter query
            // Remove multiple empty filters but keep the last added empty filter
            var emptyFields = state.FilterDefinitions.Where(x => x.Value == null && x.Operator != FilterOperator.String.Empty && x.Operator != FilterOperator.String.NotEmpty);
            for (var i = 0; i < emptyFields.Count() - 1; i++)
            {
                DataGrid!.FilterDefinitions.Remove(emptyFields.ElementAt(i));
            }

            // Convert MudBlazor's FilterDefinitions to OData query
            var filterList = state
                .FilterDefinitions
                .Select(x =>
                {
                    if (x.Value == null && x.Operator != FilterOperator.String.Empty && x.Operator != FilterOperator.String.NotEmpty)
                    {
                        return null;
                    }

                    var filterTemplate = string.Empty;
                    var field = x.Column!.PropertyName;
                    var value = x.Value;

                    if (x.Value != null)
                    {
                        if (x.FieldType.IsString)
                        {
                            value = $"'{((string)x.Value).Replace("'", "''")}'";
                        }
                        else if (x.FieldType.IsDateTime)
                        {
                            value = ((DateTime)x.Value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        }
                    }

                    switch (x.Operator)
                    {
                        case FilterOperator.Number.Equal:
                        case FilterOperator.String.Equal:
                        case FilterOperator.DateTime.Is:
                            filterTemplate = "{0} eq {1}";
                            break;
                        case FilterOperator.Number.NotEqual:
                        case FilterOperator.String.NotEqual:
                        case FilterOperator.DateTime.IsNot:
                            filterTemplate = "{0} ne {1}";
                            break;
                        case FilterOperator.Number.GreaterThan:
                        case FilterOperator.DateTime.After:
                            filterTemplate = "{0} qt {1}";
                            break;
                        case FilterOperator.Number.GreaterThanOrEqual:
                        case FilterOperator.DateTime.OnOrAfter:
                            filterTemplate = "{0} ge {1}";
                            break;
                        case FilterOperator.Number.LessThan:
                        case FilterOperator.DateTime.Before:
                            filterTemplate = "{0} lt {1}";
                            break;
                        case FilterOperator.Number.LessThanOrEqual:
                        case FilterOperator.DateTime.OnOrBefore:
                            filterTemplate = "{0} le {1}";
                            break;
                        case FilterOperator.String.Contains:
                            filterTemplate = "contains({0},{1})";
                            break;
                        case FilterOperator.String.NotContains:
                            filterTemplate = "not contains({0},{1})";
                            break;
                        case FilterOperator.String.StartsWith:
                            filterTemplate = "startswith({0},{1})";
                            break;
                        case FilterOperator.String.EndsWith:
                            filterTemplate = "endswith({0},{1})";
                            break;
                        case FilterOperator.String.Empty:
                            filterTemplate = "{0} eq null";
                            break;
                        case FilterOperator.String.NotEmpty:
                            filterTemplate = "{0} ne null";
                            break;
                        default:
                            filterTemplate = "{0} eq {1}";
                            break;
                    }

                    var filter = string.Format(filterTemplate, field, value);

                    return filter;
                })
                .Distinct()
                .Where(x => !string.IsNullOrWhiteSpace(x));

            if (filterList.Count() > 0)
            {
                builder = builder.AddQueryOption("$filter", string.Join(" and ", filterList));
            }
            #endregion

            // Convert MudBlazor's SortDefinitions to OData query

            if (state.SortDefinitions.Count > 0)
            {
                var sortList = state
                    .SortDefinitions
                    .OrderBy(x => x.Index)
                    .Select(x => x.Descending ? x.SortBy + " desc" : x.SortBy);
                builder = builder.AddQueryOption("$orderby", string.Join(',', sortList));
            }

            var builderQueryable = builder.AsQueryable();

            // apply custom filters
            if (Where != null)
            {
                builderQueryable = builderQueryable.Where(Where);
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
            GridData<T> gridData;

            try
            {
                var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url);
                if (res == null || res.Count == null)
                {
                    throw new Exception();
                }

                gridData = new GridData<T>
                {
                    Items = res.Value.ToList(),
                    TotalItems = (int)res.Count.Value,
                };

                if (_OnBeforeDataBound != null)
                {
                    _OnBeforeDataBound(this, new KeyValuePair<Guid, List<T>>(DataGridId, res.Value.ToList()));
                }
            }
            catch (Exception)
            {
                throw;
            }

            return gridData;
        }
    }
}