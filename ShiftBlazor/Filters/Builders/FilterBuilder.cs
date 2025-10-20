using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

namespace ShiftSoftware.ShiftBlazor.Filters.Builders;

public abstract class FilterBuilder<T, TProperty> : ComponentBase
{
    [Parameter]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Parameter]
    [EditorRequired]
    public Expression<Func<T, TProperty>> Property { get; set; }
    [Parameter]
    public ODataOperator? Operator { get; set; }
    [Parameter]
    public bool Hidden { get; set; }
    [Parameter]
    public bool Immediate { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public List<string>? CollectionPrefix { get; set; }

#pragma warning disable IDE1006 // Naming Styles
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
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    /// Format string to format the value, e.g. "yyyy-MM-dd" for DateTime and "C" for decimal
    /// Can only be used with DateTime and numeric types
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public int Order { get; set; } = int.MaxValue;
    [Parameter]
    public RenderFragment<FilterModelBase>? Template { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    public FilterModelBase? Filter { get; private set; }

    protected bool HasInitialized = false;
    protected bool HasChanged = false;

    protected override void OnInitialized()
    {
        if (Parent == null)
        {
            throw new Exception("Filter must be inside an IFilterableComponent");
        }

        var memberExpression = Property.Body as MemberExpression;
        var field = memberExpression?.Member;
        var isCollection = CollectionPrefix?.Count > 0;
        var path = Misc.GetPropertyPath(Property, "/");

        if (field is PropertyInfo propertyInfo)
        {
            isCollection = isCollection || propertyInfo.PropertyType.IsEnumerable();
            Filter = CreateFilter(path, propertyInfo.PropertyType);
        }
        else
        {
            throw new Exception("Could not find property");
        }

        if (Hidden &&
            Filter.Value == null &&
            Operator != ODataOperator.IsEmpty &&
            Operator != ODataOperator.IsNotEmpty)
        {
            return;
        }

        Filter.Operator = Operator ?? ODataOperator.Equal;
        Filter.Id = Id;
        Filter.IsHidden = Hidden;
        Filter.IsImmediate = Immediate;
        if (CollectionPrefix?.Count > 0)
        {
            Filter.Prefix = string.Join('/', CollectionPrefix);
        }
        Filter.IsCollection = isCollection;

        Filter.UIOptions = new FilterUIOptions
        {
            Label = Label,
            xxl = xxl,
            xl = xl,
            lg = lg,
            md = md,
            sm = sm,
            xs = xs,
            Format = Format,
            Order = Order,
            Template = Template,
        };

        Parent.Filters.Remove(Id);
        Parent.Filters.TryAdd(Id, Filter!);

        HasInitialized = true;
    }

    public override async Task SetParametersAsync(ParameterView parameters) 
    {
        if (HasInitialized)
        {
            foreach (var parameter in parameters)
            {
                var isEqual = parameter.Name switch
                {
                    nameof(Operator) => Operator == parameter.Value as ODataOperator?,
                    nameof(Hidden) => Hidden == parameter.Value as bool?,
                    nameof(Immediate) => Immediate == parameter.Value as bool?,
                    nameof(Label) => Label == parameter.Value as string,
                    nameof(xxl) => xxl == parameter.Value as int?,
                    nameof(xl) => xl == parameter.Value as int?,
                    nameof(lg) => lg == parameter.Value as int?,
                    nameof(md) => md == parameter.Value as int?,
                    nameof(sm) => sm == parameter.Value as int?,
                    nameof(xs) => xs == parameter.Value as int?,
                    nameof(Order) => Order == parameter.Value as int?,
                    nameof(Template) => Template == parameter.Value as RenderFragment<FilterModelBase>,
                    _ => true,
                };
            
                if (!isEqual)
                {
                    HasChanged = true;
                    break;
                }
            }

            parameters.TryGetValue(nameof(Parent), out IFilterableComponent? _parent);
            (_parent as IShiftList)?.ActiveOperations.Remove(Id);
        }
        else
        {
            parameters.TryGetValue(nameof(Parent), out IFilterableComponent? _parent);
            (_parent as IShiftList)?.ActiveOperations.Add(Id);
        }

        await base.SetParametersAsync(parameters);

        if (HasChanged)
        {
            UpdateFilterValues();
            HasChanged = false;
        }
    }

    protected virtual void UpdateFilterValues()
    {
        Filter!.Operator = Operator ?? ODataOperator.Equal;
        Filter.Id = Id;
        Filter.IsHidden = Hidden;
        Filter.IsImmediate = Immediate;
        Filter.UIOptions = new FilterUIOptions
        {
            Label = Label,
            xxl = xxl,
            xl = xl,
            lg = lg,
            md = md,
            sm = sm,
            xs = xs,
            Format = Format,
            Order = Order,
            Template = Template,
        };

        UpdateFilter();
    }

    protected virtual FilterModelBase CreateFilter(string path, Type propertyType)
    {
        return FilterModelBase.CreateFilter(path, propertyType, isDefault: true);
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
