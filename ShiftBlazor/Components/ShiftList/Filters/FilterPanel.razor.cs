using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public partial class FilterPanel: ComponentBase
{
    [Parameter]
    public Type? DTO { get; set; }

    [Parameter]
    public RenderFragment? FilterTempalte { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    public ODataFilterGenerator? Filter { get; private set; }

    public Dictionary<Guid, KeyValuePair<Type, Dictionary<string, object>>> FilterComponents { get; set; } = [];

    private IEnumerable<PropertyInfo> Fields = [];
    private bool IsAnd = true;
    private bool Immediate;
    internal record FilterItem(Guid Id, Type Type, Dictionary<string, object> Parameters);

    protected override void OnInitialized()
    {
        if (DTO == null)
            throw new ArgumentNullException(nameof(DTO));

        Filter = new ODataFilterGenerator(IsAnd);


        var fields = DTO.GetProperties().Where(x => x.CanWrite);
        var attr = Misc.GetAttribute<FilterableAttribute>(DTO);
        Immediate = attr?.Immediate ?? false;

        if (attr?.Disabled == true) return;

        // only get fields that have the filterable attribute
        // unless the dto itself has the filterable attribute
        // then get every field that isn't disabled using filterable attribute
        if (attr == null)
        {
            Fields = fields.Where(x => x.GetCustomAttribute<FilterableAttribute>()?.Disabled == false);
        }
        else
        {
            Fields = fields.Where(x => x.GetCustomAttribute<FilterableAttribute>()?.Disabled != true);
        }
    }

    private void AddFilter(PropertyInfo field)
    {
        var filter = CreateFilter(field, dto: DTO);
        if (filter != null)
            FilterComponents.TryAdd(filter.Id, new (filter.Type, filter.Parameters));
    }

    internal static FilterItem? CreateFilter(PropertyInfo field, Dictionary<string, object>? parameters = null, Type? dto = null)
    {
        var compType = default(Type);
        parameters ??= new Dictionary<string, object>();

        var fieldType = FieldType.Identify(field.PropertyType);
        var immediate = field.GetCustomAttribute<FilterableAttribute>()?.Immediate ?? false;
        var id = Guid.NewGuid();
        parameters.Add(nameof(FilterInput.Name), field.Name);
        parameters.Add(nameof(FilterInput.Id), id);
        parameters.Add(nameof(FilterInput.Immediate), immediate);

        if (fieldType.IsString || fieldType.IsGuid)
        {
            compType = typeof(StringFilter);
            if (dto != null)
            {
                parameters.Add(nameof(StringFilter.DtoType), dto);
            }
        }
        else if (IsDateTime(field.PropertyType))
        {
            compType = typeof(DateFilter);
        }
        else if (fieldType.IsBoolean)
        {
            compType = typeof(BooleanFilter);
        }
        else if (fieldType.IsEnum)
        {
            compType = typeof(EnumFilter);
            parameters.Add(nameof(FilterInput.FieldType), field.PropertyType);

        }
        else if (fieldType.IsNumber)
        {
            compType = typeof(NumericFilter);
            parameters.Add(nameof(FilterInput.FieldType), field.PropertyType);
        }

        if (compType != null)
        {
            return new(id, compType, parameters);
        }

        return null;
    }

    internal void ApplyFilter(ODataFilterGenerator filter, bool immediate)
    {
        Parent?.Filters.Add(filter);
        ReloadList(immediate);
    }

    private void RemoveFilterComponent(Guid id)
    {
        FilterComponents.Remove(id);
        ClearFilter(id, false);
    }

    public void ClearFilter(Guid id, bool immediate)
    {
        Parent?.Filters.Remove(id);
        ReloadList(immediate);
    }

    private void ReloadList(bool immediate)
    {
        if (Parent is IShiftList list && (Parent?.FilterImmediate == true || immediate || Immediate)) list.Reload();
    }

    public void StateChange()
    {
        StateHasChanged();
    }

    // Mud's FieldType.Identify doesn't work with DateTimeOffset
    private static bool IsDateTime(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return true;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);

        return underlyingType is not null && (underlyingType == typeof(DateTime) || type == typeof(DateTimeOffset));
    }
}
