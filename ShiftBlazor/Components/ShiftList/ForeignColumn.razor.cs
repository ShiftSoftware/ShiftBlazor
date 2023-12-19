using Microsoft.AspNetCore.Components;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ShiftBlazor.Components
{

    public partial class ForeignColumn<T, TProperty, TEntity> : PropertyColumnExtended<T, TProperty>, IDisposable
        where T : ShiftEntityDTOBase, new()
        where TEntity : ShiftEntityDTOBase, new()
    {
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string EntitySetName { get; set; }

        [Parameter]
        public string? BaseUrl { get; set; }

        [Parameter]
        public string? BaseUrlKey { get; set; }

        [Parameter]
        public string? DataValueField { get; set; }

        [Parameter]
        public string? ForeignTextField { get; set; }

        internal bool IsReady = false;
        internal List<TEntity> RemoteData { get; set; } = new();
        internal bool IsFilterOpen = false;
        internal string FilterIcon => FilterItems.Count > 0 ? Icons.Material.Filled.FilterAlt : Icons.Material.Outlined.FilterAlt;
        internal List<ShiftEntitySelectDTO> FilterItems { get; set; } = new();

        private DataServiceQuery<TEntity> QueryBuilder;
        private string TValueField = string.Empty;
        private string TEntityTextField = string.Empty;
        private string TEntityValueField = nameof(ShiftEntityDTOBase.ID);
        private bool IsForbiddenStatusCode = false;
        private bool FailedToLoadData = false;
        private string? ErrorMessage = null;

        protected override void OnInitialized()
        {
            if (string.IsNullOrWhiteSpace(EntitySetName))
                throw new ArgumentNullException(nameof(EntitySetName));

            var attr = Misc.GetAttribute<TEntity, ShiftEntityKeyAndNameAttribute>();
            TEntityTextField = ForeignTextField ?? attr?.Text ?? "";

            if (string.IsNullOrWhiteSpace(TEntityTextField))
                throw new ArgumentNullException(nameof(ForeignTextField));

            Title ??= EntitySetName;

            ShiftBlazorEvents.OnBeforeGridDataBound += OnBeforeDataBound;

            string? url = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
            QueryBuilder = OData.CreateNewQuery<TEntity>(EntitySetName, url);

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            TValueField = GetDataValueFieldName();
            KeyPropertyName ??= TValueField;

            Sortable = false;
            ShowFilterIcon = false;
            Class = "foreign-column";
            HeaderTemplate = _HeaderTemplate;
            CellTemplate = _CellTemplate;
        }

        internal void OnBeforeDataBound(object? sender, KeyValuePair<Guid, List<object>> data)
        {
            if (ShiftList.DataGridId != data.Key)
            {
                return;
            }

            FilterItems = DataGrid.FilterDefinitions
                .Where(x => !string.IsNullOrWhiteSpace(x.Column?.PropertyName) && x.Column.PropertyName == PropertyName)
                .Select(x => new ShiftEntitySelectDTO
                {
                    Value = (string)x.Value!,
                    Text = x.Title,

                }).ToList();

            RequestForeignData(data);
        }

        internal async void RequestForeignData(KeyValuePair<Guid, List<object>> data)
        {
            var items = data.Value;

            if (items != null && items.Count() > 0)
            {
                var itemIds = items
                    .Select(x => Misc.GetValueFromPropertyPath(x, TValueField)?.ToString())
                    .Distinct();

                if (itemIds.Count() > 0)
                {
                    try
                    {
                        FailedToLoadData = false;

                        var url = QueryBuilder
                            .AddQueryOption("$select", $"{TEntityValueField},{TEntityTextField}")
                            .WhereQuery(x => itemIds.Contains(x.ID))
                            .ToString();

                        var res = await Http.GetAsync(url);

                        if (res.IsSuccessStatusCode)
                        {
                            var result = await res.Content.ReadFromJsonAsync<ODataDTO<TEntity>>();

                            if (result != null)
                            {
                                RemoteData = result.Value;
                            }
                        }

                        IsForbiddenStatusCode = res.StatusCode == System.Net.HttpStatusCode.Forbidden;
                    }
                    catch (Exception e)
                    {
                        ErrorMessage = e.ToString();
                        FailedToLoadData = true;
                    }
                }
            }
            IsReady = true;
            ShiftList.GridStateHasChanged();
        }

        private string GetDataValueFieldName()
        {
            string? field = DataValueField;

            if (string.IsNullOrWhiteSpace(DataValueField) && !string.IsNullOrWhiteSpace(PropertyName) && !Guid.TryParse(PropertyName, out _))
            {
                field = Misc.GetFieldFromPropertyPath(PropertyName);
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                throw new Exception(message: $"'{nameof(DataValueField)}' cannot be null when '{nameof(Property)}' is null or is a dynamic expression");
            }

            return field;
        }

        private async Task ClearFilterAsync()
        {
            DataGrid.FilterDefinitions.RemoveAll(x => x.Column!.Identifier == Identifier);
            FilterItems.Clear();
            await DataGrid.ReloadServerData();
            CloseFilter();
        }

        private async Task ApplyFilterAsync()
        {
            var filterDefinitions = FilterItems.Select(x => new FilterDefinition<T>
            {
                Column = this,
                Operator = FilterOperator.String.Equal,
                Title = x.Text,
                Value = x.Value,
            });


            DataGrid.FilterDefinitions.RemoveAll(x => x.Column!.Identifier == Identifier);
            DataGrid.FilterDefinitions.AddRange(filterDefinitions);
            FilterItems.Clear();
            await DataGrid!.ReloadServerData();
            CloseFilter();
        }

        private void OpenFilter()
        {
            IsFilterOpen = true;
            ShiftList.GridStateHasChanged();
        }

        private void CloseFilter()
        {
            IsFilterOpen = false;
            ShiftList.GridStateHasChanged();
        }

        private void ShowErrorMessage()
        {
            var message = new Message
            {
                Title = $"{Title} Column Failed to Load",
                Body = ErrorMessage,
            };

            var parameters = new DialogParameters {
                    { "Message", message },
                    { "Color", Color.Error },
                    { "Icon", Icons.Material.Filled.Error },
                };

            DialogService.Show<PopupMessage>("", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                NoHeader = true,
            });
        }

        void IDisposable.Dispose()
        {
            ShiftBlazorEvents.OnBeforeGridDataBound -= OnBeforeDataBound;
        }
    }
}
