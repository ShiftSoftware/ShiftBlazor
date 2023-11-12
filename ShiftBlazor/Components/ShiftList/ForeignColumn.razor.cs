using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;

namespace ShiftSoftware.ShiftBlazor.Components
{

    public partial class ForeignColumn<T, TEntity, TProperty> : PropertyColumnExtended<T, TProperty>, IDisposable
        where T : ShiftEntityDTOBase, new()
        where TEntity : ShiftEntityDTOBase, new()
    {
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] MessageService MessageService { get; set; } = default!;

        #region hide properties

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use this", true)]
        public new Expression<Func<T, TProperty>> Property { get; set; }

        #endregion

        [Parameter]
        public Expression<Func<TEntity, TProperty>> EntityProperty { get; set; }

        [Parameter, EditorRequired]
        public ODataParameters<TEntity>? ODataParameters { get; set; }

        internal bool IsReady = false;
        internal List<TEntity> RemoteData { get; set; } = new();
        internal bool IsFilterOpen = false;
        internal string FilterIcon => FilterItems.Count > 0 ? Icons.Material.Filled.FilterAlt : Icons.Material.Outlined.FilterAlt;
        internal List<ShiftEntitySelectDTO> FilterItems { get; set; } = new();

        internal string PropertyFieldName = string.Empty;

        protected override void OnInitialized()
        {
            if (ODataParameters == null)
                throw new ArgumentNullException(nameof(ODataParameters));

            if (string.IsNullOrWhiteSpace(ODataParameters.EntitySetName))
                throw new ArgumentNullException(nameof(ODataParameters.EntitySetName));

            if (string.IsNullOrWhiteSpace(ODataParameters.DataValueField))
                throw new ArgumentNullException(nameof(ODataParameters.DataValueField));

            if (EntityProperty == null && string.IsNullOrWhiteSpace(ODataParameters.DataTextField))
                throw new ArgumentNullException(message: $"Both '{nameof(EntityProperty)}' and '{nameof(ODataParameters.DataTextField)}' parameters cannot be null", null);

            ShiftBlazorEvents.OnBeforeGridDataBound += OnBeforeDataBound;
            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            KeyPropertyName ??= ODataParameters?.DataValueField;
            Title ??= ODataParameters?.EntitySetName;

            base.OnParametersSet();
            PropertyFieldName = GetPropertyFieldName();

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
                .Where(x => x.Column!.Identifier == Identifier)
                .Select(x => new ShiftEntitySelectDTO
                {
                    Text = (string)x.Value!,

                }).ToList();

            RequestForeignData(data);
        }

        internal async void RequestForeignData(KeyValuePair<Guid, List<object>> data)
        {
            var items = data.Value;

            if (items != null && items.Count() > 0)
            {
                var itemIds = items
                    .Select(x => Misc.GetValueFromPropertyPath(x, ODataParameters!.DataValueField!)?.ToString())
                    .Distinct();

                if (itemIds.Count() > 0)
                {
                    try
                    {
                        var queryBuilder = ODataParameters!.QueryBuilder;
                        queryBuilder = queryBuilder.AddQueryOption("$select", $"{nameof(ShiftEntityDTOBase.ID)},{PropertyFieldName}");
                        var url = queryBuilder.Where(x => itemIds.Contains(x.ID)).ToString();
                        var result = await Http.GetFromJsonAsync<ODataDTO<TEntity>>(url);

                        if (result != null)
                        {
                            RemoteData = result.Value;
                        }
                    }
                    catch (Exception)
                    {
                        MessageService.Error("Could not load column");
                    }
                }
            }
            IsReady = true;
            ShiftList.GridStateHasChanged();
        }

        private string GetPropertyFieldName()
        {
            string? propertyName;
            if (PropertyName != null && !Guid.TryParse(PropertyName, out _))
            {
                propertyName = ODataParameters!.DataTextField = Misc.GetFieldFromPropertyPath(PropertyName);
            }
            else
            {
                propertyName = ODataParameters!.DataTextField;
            }

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new Exception(message: $"'{nameof(ODataParameters.DataTextField)}' cannot be null when '{nameof(EntityProperty)}' is null or a dynamic expression");

            return propertyName;
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
                Title = PropertyFieldName,
                Value = x.Text,
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

        void IDisposable.Dispose()
        {
            ShiftBlazorEvents.OnBeforeGridDataBound -= OnBeforeDataBound;
        }
    }
}
