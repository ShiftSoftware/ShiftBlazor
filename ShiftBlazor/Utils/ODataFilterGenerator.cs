using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ODataFilterGenerator
    {
        public readonly Guid? Id;
        public readonly List<ODataFilter> Filters;
        public readonly List<ODataFilterGenerator> ChildFilters;
        private readonly bool IsAnd;

        private const string OrLogic = "or";
        private const string AndLogic = "and";


        public ODataFilterGenerator(bool UseAndLogic = true, Guid? id = null)
        {
            Id = id;
            IsAnd = UseAndLogic;
            Filters = [];
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
            return generator.Filters.Count + count;
        }

        internal readonly static Dictionary<char, string> SpecialCharaters = new()
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

            foreach (var filter in generator.Filters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Field))
                {
                    var template = CreateFilterTemplate(filter.Operator);
                    var type = FieldType.Identify(filter.Value?.GetType());
                    var value = GetValueString(filter.Value, type);
                    filters.Add(string.Format(template, filter.Field, value));
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
                if (value is not string && value is IEnumerable list)
                {
                    var fixedList = new List<object>();
                    foreach (var item in list)
                    {
                        var type = FieldType.Identify(item?.GetType());
                        var val = GetValueString(item, type);
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
            string? filterTemplate;
            switch (filterOperator)
            {
                case ODataOperator.Equal:
                case FilterOperator.Number.Equal:
                case FilterOperator.String.Equal:
                case FilterOperator.DateTime.Is:
                    filterTemplate = "{0} eq {1}";
                    break;
                case ODataOperator.NotEqual:
                case FilterOperator.Number.NotEqual:
                case FilterOperator.String.NotEqual:
                case FilterOperator.DateTime.IsNot:
                    filterTemplate = "{0} ne {1}";
                    break;
                case ODataOperator.GreaterThan:
                case FilterOperator.Number.GreaterThan:
                case FilterOperator.DateTime.After:
                    filterTemplate = "{0} gt {1}";
                    break;
                case ODataOperator.GreaterThanOrEqual:
                case FilterOperator.Number.GreaterThanOrEqual:
                case FilterOperator.DateTime.OnOrAfter:
                    filterTemplate = "{0} ge {1}";
                    break;
                case ODataOperator.LessThan:
                case FilterOperator.Number.LessThan:
                case FilterOperator.DateTime.Before:
                    filterTemplate = "{0} lt {1}";
                    break;
                case ODataOperator.LessThanOrEqual:
                case FilterOperator.Number.LessThanOrEqual:
                case FilterOperator.DateTime.OnOrBefore:
                    filterTemplate = "{0} le {1}";
                    break;
                case ODataOperator.Contains:
                case FilterOperator.String.Contains:
                    filterTemplate = "contains({0},{1})";
                    break;
                case ODataOperator.NotContains:
                case FilterOperator.String.NotContains:
                    filterTemplate = "not contains({0},{1})";
                    break;
                case ODataOperator.StartsWith:
                case FilterOperator.String.StartsWith:
                    filterTemplate = "startswith({0},{1})";
                    break;
                case ODataOperator.EndsWith:
                case FilterOperator.String.EndsWith:
                    filterTemplate = "endswith({0},{1})";
                    break;
                case ODataOperator.In:
                    filterTemplate = "{0} in ({1})";
                    break;
                case FilterOperator.String.Empty:
                    filterTemplate = "{0} eq null";
                    break;
                case FilterOperator.String.NotEmpty:
                    filterTemplate = "{0} ne null";
                    break;
                default:
                    filterTemplate = "{0} eq {1}";
                    break;
            }

            return filterTemplate;
        }
    }
}
