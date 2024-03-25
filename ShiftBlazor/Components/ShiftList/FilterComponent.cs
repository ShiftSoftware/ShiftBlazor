using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components;

public class FilterComponent<T, TProperty> : ComponentBase where T : ShiftEntityDTOBase, new()
{

    [Parameter]
    [EditorRequired]
    public Expression<Func<T, TProperty>> Property { get; set; }

    [Parameter]
    public ODataOperator Operator { get; set; } = ODataOperator.Equal;

    [Parameter]
    public object? Value { get; set; }

    [Parameter]
    public bool Removable { get; set; } = false;

    [CascadingParameter]
    public IFilterableComponent? FilterableComponent { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        var memberExpression = Property.Body as MemberExpression;
        var fieldName = memberExpression?.Member.Name;
        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            FilterableComponent?.AddFilter(fieldName, Operator, Value);
        }
    }
}
