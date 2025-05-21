using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Components;

namespace ShiftSoftware.ShiftBlazor.Filters.Builders;

public abstract class FilterBuilder<T, TProperty> : ComponentBase
{
    [Parameter]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Parameter]
    [EditorRequired]
    public Expression<Func<T, TProperty>> Property { get; set; }
    [Parameter]
    public ODataOperator Operator { get; set; }
    [Parameter]
    public bool Hidden { get; set; }
    [Parameter]
    public bool ReadOnly { get; set; }
    [Parameter]
    public bool Immediate { get; set; }

    [Parameter]
    public int xxl { get; set; }
    [Parameter]
    public int xl { get; set; }
    [Parameter]
    public int lg { get; set; }
    [Parameter]
    public int md { get; set; }
    [Parameter]
    public int sm { get; set; }
    [Parameter]
    public int xs { get; set; }
    [Parameter]
    public int Order { get; set; } = int.MaxValue;
    [Parameter]
    public RenderFragment<FilterModelBase>? Template { get; set; }


    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    protected FilterModelBase? Filter { get; set; }
    protected bool IsInitialized = false;

    private readonly Dictionary<string, object?> _previousParameters = new();

    protected override void OnInitialized()
    {
        //if (Parent is IShiftList)
        {
            var memberExpression = Property.Body as MemberExpression;
            var field = memberExpression?.Member;

            if (field is PropertyInfo propertyInfo)
            {
                Filter = CreateFilter(propertyInfo);
            }
            else
            {
                throw new Exception("Could not find property");
            }

            Filter.Operator = Operator;
            Filter.Id = Id;
            Filter.IsHidden = Hidden;
            Filter.IsDisabled = ReadOnly;
            Filter.IsImmediate = Immediate;

            Filter.UIOptions = new FilterUIOptions
            {
                xxl = xxl,
                xl = xl,
                lg = lg,
                md = md,
                sm = sm,
                xs = xs,
                Order = Order,
                Template = Template,
            };

            Parent.Filters.Remove(Id);
            Parent.Filters.TryAdd(Id, Filter!);

            IsInitialized = true;
        }
        //else
        //{
        //    throw new Exception("Parent is not IShiftList");
        //}
    }

    protected virtual void OnParametersChanged()
    {
        Filter.Operator = Operator;
        Filter.Id = Id;
        Filter.IsHidden = Hidden;
        Filter.IsDisabled = ReadOnly;
        Filter.IsImmediate = Immediate;
    }

    private bool HasParametersChanged(ParameterView parameters)
    {
        var hasChanged = false;
        var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
            .Where(p => p.Name != nameof(Property));

        foreach (var prop in props)
        {
            var name = prop.Name;
            if (!parameters.TryGetValue(name, out object? newValue))
                continue;

            _previousParameters.TryGetValue(name, out var oldValue);

            if (!Equals(newValue, oldValue))
            {
                _previousParameters[name] = newValue;
                hasChanged = true;
            }
        }

        return hasChanged;
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        bool hasChanged = false;
        if (IsInitialized)
        {
            hasChanged = HasParametersChanged(parameters);
        }

        await base.SetParametersAsync(parameters);

        if (IsInitialized && hasChanged)
        {
            OnParametersChanged();
            UpdateFilter();
        }
    }

    protected virtual FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        return FilterModelBase.CreateFilter(propertyInfo, isDefault: true);
    }

    protected void UpdateFilterValue<T>(T value)
    {
        Filter!.Value = value;
        UpdateFilter();
    }

    public void UpdateFilter()
    {
        if (Parent != null)
        {
            Parent.Filters.Remove(Id);
            Parent.Filters.TryAdd(Id, Filter!);
            ReloadList();
        }
    }

    private bool ReloadList(bool immediate = false)
    {
        if (Parent is IShiftList list && (Parent?.FilterImmediate == true || immediate || Immediate))
        {
            list.Reload();
            return true;
        }

        return false;
    }

    protected override bool ShouldRender() => false;
}
