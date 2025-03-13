﻿using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Collections;
using System.Collections.Generic;
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

    public readonly static Dictionary<char, string> SpecialCharaters = new()
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
        var found = ChildFilters.FirstOrDefault(x => generator.Id != null && x.Id == generator.Id);

        if (found != null)
        {
            ChildFilters.Remove(found);
        }

        ChildFilters.Add(generator);
        return this;
    }

    public ODataFilterGenerator Add(ODataFilter filter)
    {
        var found = Filters.FirstOrDefault(x => filter.Id != null && x.Id == filter.Id);

        if (found != null)
        {
            Filters.Remove(found);
        }

        Filters.Add(filter);
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

    public ODataFilterGenerator And(Action<ODataFilterGenerator> filterConfig, Guid? id = null)
    {
        var found = ChildFilters.FirstOrDefault(x => id != null && x.Id == id);

        if (found != null)
        {
            ChildFilters.Remove(found);
        }

        var generator = new ODataFilterGenerator(true);
        filterConfig.Invoke(generator);
        ChildFilters.Add(generator);
        return this;
    }

    public ODataFilterGenerator Or(Action<ODataFilterGenerator> filterConfig, Guid? id = null)
    {
        var found = ChildFilters.FirstOrDefault(x => id != null && x.Id == id);

        if (found != null)
        {
            ChildFilters.Remove(found);
        }

        var generator = new ODataFilterGenerator(false);
        filterConfig.Invoke(generator);
        ChildFilters.Add(generator);
        return this;
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
                var field =  string.IsNullOrWhiteSpace(filter.CastToType) ? filter.Field : $"cast({filter.Field}, '{filter.CastToType}')";
                var template = CreateFilterTemplate(filter.Operator);
                var value = GetValueString(filter.Value);
                filters.Add(string.Format(template, field, value));
            }
        }

        foreach (var childGenerator in generator.ChildFilters)
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
            if (SpecialCharaters.TryGetValue(x, out string? escaped))
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
            ODataOperator.NotContains or FilterOperator.String.NotContains => "not contains({0},{1})",
            ODataOperator.StartsWith or FilterOperator.String.StartsWith => "startswith({0},{1})",
            ODataOperator.EndsWith or FilterOperator.String.EndsWith => "endswith({0},{1})",
            ODataOperator.IsEmpty or FilterOperator.String.Empty => "{0} eq null",
            ODataOperator.IsNotEmpty or FilterOperator.String.NotEmpty => "{0} ne null",
            ODataOperator.In => "{0} in ({1})",
            _ => "{0} eq {1}",
        };
        return filterTemplate;
    }
}
