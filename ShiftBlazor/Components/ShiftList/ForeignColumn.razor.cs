using Microsoft.AspNetCore.Components;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components
{

    public partial class ForeignColumn<T, TProperty, TEntity> : PropertyColumnExtended<T, TProperty>, IDisposable, IForeignColumn
        where T : ShiftEntityDTOBase, new()
        where TEntity : ShiftEntityDTOBase, new()
    {
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string EntitySet { get; set; }

        [Parameter]
        public string? BaseUrl { get; set; }

        [Parameter]
        public string? BaseUrlKey { get; set; }
        
        [Parameter]
        public string ODataPath { get; set; } = "api";

        [Parameter]
        public string? DataValueField { get; set; }

        [Parameter]
        public string? ForeignTextField { get; set; }

        public string? Url { get; private set; }
        public string TEntityTextField { get; private set; } = string.Empty;
        public string TEntityValueField { get; private set; } = nameof(ShiftEntityDTOBase.ID);

        internal bool IsReady = false;
        internal List<TEntity> RemoteData { get; set; } = new();
        internal bool IsFilterOpen = false;
        internal string FilterIcon => FilterItems.Count > 0 ? Icons.Material.Filled.FilterAlt : Icons.Material.Outlined.FilterAlt;
        internal List<ShiftEntitySelectDTO> FilterItems { get; set; } = new();

        private DataServiceQuery<TEntity> QueryBuilder { get; set; }
        private string? TValueField = null;
        private bool IsForbiddenStatusCode = false;
        private bool FailedToLoadData = false;
        private string? ErrorMessage = null;

        protected override void OnInitialized()
        {
            if (string.IsNullOrWhiteSpace(EntitySet))
                throw new ArgumentNullException(nameof(EntitySet));

            var attr = Misc.GetAttribute<TEntity, ShiftEntityKeyAndNameAttribute>();
            TEntityTextField = ForeignTextField ?? attr?.Text ?? "";

            if (string.IsNullOrWhiteSpace(TEntityTextField))
                throw new ArgumentNullException(nameof(ForeignTextField));

            Title ??= EntitySet;

            ShiftBlazorEvents.OnBeforeGridDataBound += OnBeforeDataBound;

            Url = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
            Url = Url?.AddUrlPath(this.ODataPath);
            QueryBuilder = OData.CreateNewQuery<TEntity>(EntitySet, Url);

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            TValueField ??= IForeignColumn.GetDataValueFieldName(this);
            KeyPropertyName ??= TValueField;

            ShowFilterIcon = false;

            if (Class?.Contains("foreign-column") != true)
            {
                Class ??= "";
                Class += " foreign-column";
            }

            HeaderTemplate = _HeaderTemplate;
            CellTemplate = _CellTemplate;
        }

        internal void OnBeforeDataBound(object? sender, KeyValuePair<Guid, List<object>> data)
        {
            if (ShiftList.Id != data.Key)
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

            if (items != null && items.Any())
            {
                FailedToLoadData = false;

                try
                {
                    var itemIds = IForeignColumn.GetForeignIds(this, items);
                    var foreignData = await IForeignColumn.GetForeignColumnValues<TEntity>(this, itemIds, OData, Http);
                    if (foreignData != null)
                    {
                        RemoteData = foreignData.ToList();
                    }
                }
                catch (Exception e)
                {
                    ErrorMessage = e.ToString();
                    FailedToLoadData = true;
                }
            }
            IsReady = true;
            ShiftList.GridStateHasChanged();
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
