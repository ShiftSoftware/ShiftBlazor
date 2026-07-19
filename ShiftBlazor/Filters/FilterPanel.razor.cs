using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Reflection;

namespace ShiftSoftware.ShiftBlazor.Filters;

public partial class FilterPanel : ComponentBase, IDisposable
{
    [Inject] private SettingManager SettingManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private MessageService MessageService { get; set; } = default!;

    [Parameter]
    public Type? DTO { get; set; }

    [Parameter]
    public RenderFragment? FilterTempalte { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    private IShiftList? ShiftList { get; set; }

    public ODataFilterGenerator? ODataFilters { get; private set; }

    private readonly IEnumerable<PropertyInfo> Fields = [];
    private readonly bool IsAnd = true;
    private bool IsLoading { get; set; }

    public record BasicFilter(string Field, ODataOperator Operator, string? Value);
    public record SavedFilter(string Name, Color Color, List<BasicFilter> Filters);
    private List<SavedFilter> savedFilters = [];

    protected override void OnInitialized()
    {
        ODataFilters = new ODataFilterGenerator(IsAnd);

        // if this component is used inside a ShiftList,
        // we assign it to the global ShiftList variable
        // so we can reload the list when filters change
        // and also subscribe to the OnBeforeGridDataBound event
        if (Parent is IShiftList list)
        {
            ShiftList = list;
            ShiftBlazorEvents.OnBeforeGridDataBound += OnListFetched;
            savedFilters = SettingManager.GetSavedFilters(ShiftList.GetIdentifier()) ?? [];
        }
    }

    private static string DescribeFilter(BasicFilter filter)
    {
        return string.IsNullOrEmpty(filter.Value)
            ? $"{filter.Field} {filter.Operator.Describe()}"
            : $"{filter.Field} {filter.Operator.Describe()} {filter.Value}";
    }

    private void RemoveFilterComponent(Guid id)
    {
        ClearFilter(id, true);
    }

    public void ClearFilter(Guid id, bool immediate)
    {
        Parent?.Filters.Remove(id);
        StateHasChanged();
        ReloadList(immediate);
    }

    private void SetFilterAndReload()
    {
        ReloadList(true);
    }

    private async Task OpenSaveFilterDialog()
    {
        var currentFilters = Parent?.Filters
            .Where(x => !x.Value.IsHidden)
            .Where(x => x.Value.HasValue() || x.Value.IsNoValueOperator)
            .Select(x => new BasicFilter(x.Value.Field, x.Value.Operator, x.Value.ValueToString()))
            .ToList() ?? [];

        if (currentFilters.Count == 0)
        {
            MessageService.Warning("There are no filters to save.");
            return;
        }

        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        var parameters = new DialogParameters<SaveFilterDialog> { { x => x.Filters, currentFilters } };
        var dialog = await DialogService.ShowAsync<SaveFilterDialog>("", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled || result.Data is not SavedFilter saved)
            return;

        // Replace any existing saved filter with the same name (case-insensitive).
        savedFilters.RemoveAll(x => string.Equals(x.Name, saved.Name, StringComparison.OrdinalIgnoreCase));
        savedFilters.Insert(0, saved);

        PersistSavedFilters();
        StateHasChanged();
    }

    private void RemoveSavedFilter(SavedFilter savedFilter)
    {
        savedFilters.Remove(savedFilter);
        PersistSavedFilters();
    }

    private void PersistSavedFilters()
    {
        if (ShiftList != null)
            SettingManager.SetSavedFilters(ShiftList.GetIdentifier(), savedFilters);
    }

    private void RestoreFilters(SavedFilter savedFilter)
    {
        var filterSet = savedFilter.Filters;

        if (Parent == null)
            return;

        foreach (var item in Parent.Filters)
        {
            item.Value.Value = null;
        }

        var currentFilterLookup = Parent.Filters
            .Where(x => !x.Value.IsHidden)
            .ToLookup(x => x.Value.Field, x => x.Value);

        var filterHistoryLookup = filterSet.ToLookup(x => x.Field, x => x);
        var hasFailure = false;

        foreach (var filterHistoryGroup in filterHistoryLookup)
        {
            var field = filterHistoryGroup.Key;
            var currentFilterGroup = currentFilterLookup[field].ToList();

            foreach (var (a, b) in currentFilterGroup.Zip(filterHistoryGroup))
            {
                try
                {
                    // Operator must be set before ParseValue.
                    a.Operator = b.Operator;
                    a.Value = a.ParseValue(b.Value);
                }
                catch
                {
                    a.Value = null;
                    hasFailure = true;
                }
            }
        }

        if (hasFailure)
            RemoveSavedFilter(savedFilter);

        StateHasChanged();
        ReloadList(true);
    }

    private void ReloadList(bool immediate)
    {
        if (ShiftList != null && (Parent?.FilterImmediate == true || immediate))
        {
            IsLoading = true;
            ShiftList.Reload();
        }
    }

    private void OnListFetched(object? sender, KeyValuePair<Guid, List<object>> args)
    {
        if (ShiftList?.Id == args.Key)
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void Reset()
    {
        if (Parent == null)
            return;

        foreach (var filter in Parent.Filters.Values.Where(x => !x.IsHidden))
        {
            filter.Reset();
        }
        ReloadList(true);
    }

    public void Dispose()
    {
        if (ShiftList != null)
        {
            ShiftBlazorEvents.OnBeforeGridDataBound -= OnListFetched;
        }
        GC.SuppressFinalize(this);
    }
}