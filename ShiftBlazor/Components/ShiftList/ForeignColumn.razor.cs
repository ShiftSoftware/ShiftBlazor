using Microsoft.AspNetCore.Components;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

// TODO
// - Add loading indicator when data is being fetched
// - Add caching mechanism to avoid redundant data fetches
// - refactor RequestForeignData

public partial class ForeignColumn<T, TProperty, TEntity> : PropertyColumnExtended<T, TProperty>, IDisposable, IForeignColumn, IODataRequestComponent<TEntity>
    where T : ShiftEntityDTOBase, new()
    where TEntity : ShiftEntityDTOBase, new()
{
    [Inject] public HttpClient HttpClient { get; private set; } = default!;
    [Inject] public ShiftBlazorLocalizer Loc  { get; private set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] ODataQuery OData { get; set; } = default!;
    [Inject] public SettingManager SettingManager { get; private set; } = default!;

    [Parameter]
    [EditorRequired]
    public string EntitySet { get; set; }

    [Parameter]
    public string? Endpoint { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }
    
    [Parameter]
    public string? DataValueField { get; set; }

    [Parameter]
    public string? ForeignTextField { get; set; }

    [Parameter]
    public string? ForeignEntiyField { get; set; }

    [Parameter]
    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }
    [Parameter]
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }
    [Parameter]
    public Func<Exception, ValueTask<bool>>? OnError { get; set; }
    [Parameter]
    public Func<ODataDTO<TEntity>?, ValueTask<bool>>? OnResult { get; set; }

    public string? Url { get; private set; }
    public string TEntityTextField { get; private set; } = string.Empty;
    public string TEntityValueField { get; private set; } = nameof(ShiftEntityDTOBase.ID);

    internal bool IsReady = false;
    internal List<TEntity> RemoteData { get; set; } = [];
    internal bool IsFilterOpen = false;
    internal string FilterIcon => FilterItems.Count > 0 ? Icons.Material.Filled.FilterAlt : Icons.Material.Outlined.FilterAlt;
    internal List<ShiftEntitySelectDTO> FilterItems { get; set; } = [];

    private string? TValueField = null;
    // IsForbiddenStatusCode is not being used currently, should be reimplemented with the refactor
    private bool IsForbiddenStatusCode;
    private bool FailedToLoadData;
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

        Url = IODataRequestComponent<T>.GetPath(this);

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

        _ = RequestForeignData(data);
    }

    internal async Task RequestForeignData(KeyValuePair<Guid, List<object>> data)
    {
        var items = data.Value;

        if (items?.Count > 0)
        {
            FailedToLoadData = false;

            try
            {
                var itemIds = IForeignColumn.GetForeignIds(this, items);
                var foreignData = await IForeignColumn.GetForeignColumnValues<TEntity>(this, itemIds, OData, HttpClient);

                if (OnResult != null && await OnResult.Invoke(foreignData))
                    return;

                if (foreignData != null)
                {
                    RemoteData = foreignData.Value;

                    if (ForeignEntiyField is not null)
                    {
                        foreach (var item in items)
                        {
                            var property = item.GetType().GetProperty(ForeignEntiyField);

                            if (property is not null && property.CanWrite)
                            {
                                var id = Misc.GetValueFromPropertyPath(item, TValueField!)?.ToString();
                                var thisData = RemoteData.FirstOrDefault(x => x.ID == id);

                                if (!IsForbiddenStatusCode && thisData is not null)
                                {
                                    property.SetValue(item, foreignData.Value.FirstOrDefault(x => x.ID == id));
                                }
                            }
                        }
                    }
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

        DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            NoHeader = true,
        });
    }

    public override void Dispose()
    {
        ShiftBlazorEvents.OnBeforeGridDataBound -= OnBeforeDataBound;
        GC.SuppressFinalize(this);
    }
}
