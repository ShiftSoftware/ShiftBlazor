using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using System.Collections;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Utils;

public class ODataFilterGenerator
{
    public readonly Guid? Id;
    public readonly List<ODataFilter> Filters;
    public readonly List<string> RawFilters;
    public readonly List<ODataFilterGenerator> ChildFilters;
    private readonly bool IsAnd;

    private const string OrLogic = "or";
    private const string AndLogic = "and";


    public ODataFilterGenerator(bool UseAndLogic = true, Guid? id = null)
    {
        Id = id;
        IsAnd = UseAndLogic;
        Filters = [];
        RawFilters = [];
        ChildFilters = [];
    }

    public int Count => GetCount(this);

    private int GetCount(ODataFilterGenerator generator)
    {
        var count = 0;
        foreach (var gen in generator.ChildFilters)
        {
            count += GetCount(gen);
        }
        return generator.Filters.Count + generator.RawFilters.Count + count;
    }

    public readonly static Dictionary<char, string> SpecialCharacters = new()
    {
        {'%', "%25"},
        {'+', "%2B"},
        {'/', "%2F"},
        {'?', "%3F"},
        {'#', "%23"},
        {'&', "%26"},
    };

    public ODataFilterGenerator Add(ODataFilterGenerator generator)
    {
        Remove(generator.Id);
        if (generator.Count > 0)
        {
            ChildFilters.Add(generator);
        }
        return this;
    }

    public ODataFilterGenerator Add(ODataFilter filter)
    {
        Remove(filter.Id);

        Filters.Add(filter);
        return this;
    }

    public ODataFilterGenerator Add(IEnumerable<ODataFilter> filters)
    {
        foreach (var filter in filters)
        {
            Add(filter);
        }

        return this;
    }

    public ODataFilterGenerator Add(Action<ODataFilter> filterConfig)
    {
        var _filter = new ODataFilter();
        filterConfig.Invoke(_filter);
        return Add(_filter);
    }

    public ODataFilterGenerator Add(string field, ODataOperator Operator, object? Value, Guid? id = null)
    {
        return Add(x =>
        {
            x.Id = id;
            x.Field = field;
            x.Operator = Operator;
            x.Value = Value;
        });
    }

    public ODataFilterGenerator Add(string rawFilter)
    {
        RawFilters.Add(rawFilter);
        return this;
    }

    public ODataFilterGenerator Add(FilterModelBase? filter)
    {
        if (filter == null)
        {
            return this;
        }

        if (filter.Value == null || filter.Value is IEnumerable list && list.Cast<object>().Count() == 0)
        {
            Remove(filter.Id);
        }
        else if (filter.Value != null)
        {
            return Add(x =>
            {
                x.Id = filter.Id;
                x.Field = filter.Field;
                x.Operator = filter.Operator;
                x.Value = filter.Value;
                x.IsCollection = filter.IsCollection;
                x.Prefix = filter.Prefix;
            });
        }

        return this;
    }

    public ODataFilterGenerator And(Action<ODataFilterGenerator> filterConfig, Guid? id = null)
    {
        var generator = new ODataFilterGenerator(true, id);
        filterConfig.Invoke(generator);
        return Add(generator);
    }

    public ODataFilterGenerator Or(Action<ODataFilterGenerator> filterConfig, Guid? id = null)
    {

        var generator = new ODataFilterGenerator(false, id);
        filterConfig.Invoke(generator);
        return Add(generator);
    }

    public ODataFilterGenerator AddIf(bool condition, ODataFilter filter)
    {
        if (condition)
            return Add(filter);

        return this;
    }

    public ODataFilterGenerator AddIf(bool condition, Action<ODataFilter> filterConfig)
    {
        if (condition)
            return Add(filterConfig);

        return this;
    }

    public ODataFilterGenerator AddIf(bool condition, string field, ODataOperator Operator, object? Value, Guid? id = null)
    {
        return AddIf(condition, x =>
        {
            x.Id = id;
            x.Field = field;
            x.Operator = Operator;
            x.Value = Value;
        });
    }

    public ODataFilterGenerator AndIf(bool condition, Action<ODataFilterGenerator> filterConfig)
    {
        if (condition)
            return And(filterConfig);

        return this;
    }

    public ODataFilterGenerator OrIf(bool condition, Action<ODataFilterGenerator> filterConfig)
    {
        if (condition)
            return Or(filterConfig);

        return this;
    }

    public void Remove(Guid? id)
    {
        var found = Filters.FirstOrDefault(x => id != null && x.Id == id);
        var found2 = ChildFilters.FirstOrDefault(x => id != null && x.Id == id);

        if (found != null)
        {
            Filters.Remove(found);
        }
        if (found2 != null)
        {
            ChildFilters.Remove(found2);
        }

    }

    public override string ToString()
    {
        return BuildQueryString(this);
    }

    private static string BuildQueryString(ODataFilterGenerator generator)
    {
        var filters = new List<string>();
        var combineLogic = generator.IsAnd ? AndLogic : OrLogic;

        filters.AddRange(generator.RawFilters);

        foreach (var filter in generator.Filters)
        {
            if (!string.IsNullOrWhiteSpace(filter.Field))
            {
                var field = filter.Field;
                var template = CreateFilterTemplate(filter.Operator);
                var value = GetValueString(filter.Value);

                var hasPrefix = !string.IsNullOrWhiteSpace(filter.Prefix);

                if (filter.IsCollection)
                {
                    var prefix = hasPrefix ? filter.Prefix : filter.Field;
                    field = hasPrefix ? $"item/{filter.Field}" : "item"; 
                    template = $"{prefix}/any(item: {template})";
                }
                else if (hasPrefix)
                {
                    field = $"{filter.Prefix}/{field}";
                }

                // add the odata type casting method
                if (filter.CastToType != null)
                {
                    field = CastToType(field, filter.CastToType);
                }

                filters.Add(string.Format(template, field, value));
            }
        }

        foreach (var childGenerator in generator.ChildFilters.Where(x => x.Count > 0))
        {
            var childFilters = BuildQueryString(childGenerator);
            if (!string.IsNullOrWhiteSpace(childFilters))
                filters.Add($"({childFilters})");
        }

        return string.Join($" {combineLogic} ", filters);
    }

    internal static string GetValueString(object? value, FieldType? fieldType = null)
    {
        var valString = "null";

        if (value != null)
        {
            // Check if the value is a list
            // to be used with In operator
            if (value is not string && value is IEnumerable list)
            {
                var fixedList = new List<object>();
                foreach (var item in list)
                {
                    var val = GetValueString(item);
                    if (val != null)
                        fixedList.Add(val);
                }
                return string.Join(',', fixedList);
            }

            fieldType ??= FieldType.Identify(value.GetType());

            if (fieldType.IsString)
            {
                valString = $"'{((string)value!).Replace("'", "''")}'";
            }
            else if (fieldType.IsEnum)
            {
                valString = $"'{value}'";
            }
            else if (fieldType.IsDateTime)
            {
                valString = ((DateTime)value!).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
            }
            else if (fieldType.IsBoolean)
            {
                valString = value.ToString()?.ToLower()!;
            }
            else
            {
                valString = value.ToString()!;
            }
        }

        var stringBuilder = new StringBuilder();

        foreach (char x in valString)
        {
            if (SpecialCharacters.TryGetValue(x, out string? escaped))
            {
                stringBuilder.Append(escaped);
            }
            else
            {
                stringBuilder.Append(x);
            }
        }

        return stringBuilder.ToString();
    }

    internal static string CastToType(string field, string type)
    {
        return string.IsNullOrWhiteSpace(type) ? field : $"cast({field}, '{type}')";
    }

    internal static string CreateFilterTemplate(object? filterOperator)
    {
        string? filterTemplate = filterOperator switch
        {
            ODataOperator.Equal or FilterOperator.Number.Equal or FilterOperator.String.Equal or FilterOperator.DateTime.Is => "{0} eq {1}",
            ODataOperator.NotEqual or FilterOperator.Number.NotEqual or FilterOperator.String.NotEqual or FilterOperator.DateTime.IsNot => "{0} ne {1}",
            ODataOperator.GreaterThan or FilterOperator.Number.GreaterThan or FilterOperator.DateTime.After => "{0} gt {1}",
            ODataOperator.GreaterThanOrEqual or FilterOperator.Number.GreaterThanOrEqual or FilterOperator.DateTime.OnOrAfter => "{0} ge {1}",
            ODataOperator.LessThan or FilterOperator.Number.LessThan or FilterOperator.DateTime.Before => "{0} lt {1}",
            ODataOperator.LessThanOrEqual or FilterOperator.Number.LessThanOrEqual or FilterOperator.DateTime.OnOrBefore => "{0} le {1}",
            ODataOperator.Contains or FilterOperator.String.Contains => "contains({0},{1})",
            ODataOperator.NotContains or FilterOperator.String.NotContains => "contains({0},{1}) eq false",
            ODataOperator.StartsWith or FilterOperator.String.StartsWith => "startswith({0},{1})",
            ODataOperator.NotStartsWith=> "startswith({0},{1}) eq false",
            ODataOperator.EndsWith or FilterOperator.String.EndsWith => "endswith({0},{1})",
            ODataOperator.NotEndsWith => "endswith({0},{1}) eq false",
            ODataOperator.IsEmpty or FilterOperator.String.Empty => "{0} eq null",
            ODataOperator.IsNotEmpty or FilterOperator.String.NotEmpty => "{0} ne null",
            ODataOperator.In => "{0} in ({1})",
            ODataOperator.NotIn => "not ({0} in ({1}))",
            _ => "{0} eq {1}",
        };

        return filterTemplate;
    }
}
