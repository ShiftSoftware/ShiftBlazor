using Microsoft.AspNetCore.Components;
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

    private const int MaxHistorySize = 10;
    public record BasicFilter(string Field, ODataOperator Operator, string? Value);
    private List<List<BasicFilter>> filterHistory = [];

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
            filterHistory = SettingManager.GetFilterHistory(ShiftList.GetIdentifier()) ?? [];
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
        SaveFilters();
        ReloadList(true);
    }

    private void SaveFilters()
    {
        var filtersToSave = Parent?.Filters
            .Where(x => !x.Value.IsHidden)
            .Where(x => x.Value.HasValue() || x.Value.IsNoValueOperator)
            .Select(x => new BasicFilter(x.Value.Field, x.Value.Operator, x.Value.ValueToString()))
            .ToList();

        if (filtersToSave == null || filtersToSave.Count == 0)
            return;

        var isDuplicate = filterHistory.Any(existingFilters =>
            filtersToSave.Count == existingFilters.Count && filtersToSave.All(x =>
                existingFilters.Any(z => z.Field == x.Field && z.Operator == x.Operator && z.Value == x.Value)));

        if (isDuplicate)
            return;

        // newest first
        filterHistory.Insert(0, filtersToSave);

        if (filterHistory.Count > MaxHistorySize)
            filterHistory.RemoveRange(MaxHistorySize, filterHistory.Count - MaxHistorySize);

        PersistHistory();
    }

    private void RemoveFromHistory(List<BasicFilter> filterSet)
    {
        filterHistory.Remove(filterSet);
        PersistHistory();
    }

    private void PersistHistory()
    {
        if (ShiftList != null)
            SettingManager.AddFilterToHistory(ShiftList.GetIdentifier(), filterHistory);
    }

    private void RestoreFilters(List<BasicFilter> filterSet)
    {
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
            RemoveFromHistory(filterSet);

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