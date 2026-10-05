using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Filters;

/// <summary>Supplies field-specific query controls without coupling the grid to a feature.</summary>
public interface IFieldQueryPolicy
{
    FieldQueryRule? GetRule(Type dtoType, string path);
}

public sealed record FieldQueryRule(IReadOnlyList<ODataOperator> Operators, bool Sortable)
{
    internal static FieldQueryRule? Resolve(IServiceProvider services, Type type, string path)
        => services.GetServices<IFieldQueryPolicy>().Select(p => p.GetRule(type, path)).FirstOrDefault(r => r is not null);

    internal HashSet<string> GridOperators => Operators.Select(op => op switch
    {
        ODataOperator.Equal => FilterOperator.String.Equal,
        ODataOperator.NotEqual => FilterOperator.String.NotEqual,
        ODataOperator.Contains => FilterOperator.String.Contains,
        ODataOperator.NotContains => FilterOperator.String.NotContains,
        ODataOperator.StartsWith => FilterOperator.String.StartsWith,
        ODataOperator.EndsWith => FilterOperator.String.EndsWith,
        ODataOperator.IsEmpty => FilterOperator.String.Empty,
        ODataOperator.IsNotEmpty => FilterOperator.String.NotEmpty,
        _ => throw new InvalidOperationException("The field policy contains an unsupported grid text operator.")
    }).ToHashSet();
}
