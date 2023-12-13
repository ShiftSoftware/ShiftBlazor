using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Collections;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ODataFilter
    {
        public ODataFilter(bool UseAndLogic = true)
        {
            IsAnd = UseAndLogic;
        }

        public ODataFilter(string field, ODataOperator? oDataOperator = null, object? value = null, bool UseAndLogic = true)
            : this(UseAndLogic)
        {
            Field = field;
            Operator = oDataOperator ?? Operator;
            Value = value;
        }

        private const string OrLogic = "or";
        private const string AndLogic = "and";

        public string? Field { get; set; }
        public ODataOperator Operator { get; set; } = ODataOperator.Equal;

        public object? Value { get; set; }

        private readonly bool IsAnd;
        private List<object> Filters = new List<object>();

        public ODataFilter Add(ODataFilter filter)
        {
            Filters.Add(filter);
            return this;
        }

        public ODataFilter Add(Action<ODataFilter> filterConfig)
        {
            var _filter = new ODataFilter();
            filterConfig.Invoke(_filter);
            return Add(_filter);
        }

        public ODataFilter Add(string field, ODataOperator Operator, object? Value)
        {
            return Add(x =>
            {
                x.Field = field;
                x.Operator = Operator;
                x.Value = Value;
            });
        }

        public ODataFilter And(params Action<ODataFilter>[] filterConfigs)
        {
            var andFilters = new AndList<ODataFilter>();
            andFilters.AddRange(GetFilters(filterConfigs));

            Filters.Add(andFilters);
            return this;
        }

        public ODataFilter Or(params Action<ODataFilter>[] filterConfigs)
        {
            var orFilters = new OrList<ODataFilter>();
            orFilters.AddRange(GetFilters(filterConfigs));

            Filters.Add(orFilters);
            return this;
        }

        public ODataFilter AddIf(bool condition, ODataFilter filter)
        {
            if (condition)
                return Add(filter);

            return this;
        }

        public ODataFilter AddIf(bool condition, Action<ODataFilter> filterConfig)
        {
            if (condition)
                return Add(filterConfig);

            return this;
        }

        public ODataFilter AndIf(bool condition, params Action<ODataFilter>[] filterConfigs)
        {
            if (condition)
                return And(filterConfigs);

            return this;
        }

        public ODataFilter OrIf(bool condition, params Action<ODataFilter>[] filterConfigs)
        {
            if (condition)
                return Or(filterConfigs);

            return this;
        }

        private static IEnumerable<ODataFilter> GetFilters(params Action<ODataFilter>[] filterConfigs)
        {
            return filterConfigs.Select(config =>
            {
                var filter = new ODataFilter();
                config.Invoke(filter);
                return filter;
            });
        }

        public override string ToString()
        {
            return BuildQueryString(new[] { this }, IsAnd);
        }

        private static string BuildQueryString(IEnumerable<object> filterList, bool isAnd = true)
        {
            var filters = new List<string>();
            var combineLogic = isAnd ? AndLogic : OrLogic;

            foreach (var item in filterList)
            {
                if (item is ODataFilter filter)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Field))
                    {
                        var template = CreateFilterTemplate(filter.Operator);
                        var type = FieldType.Identify(filter.Value?.GetType());
                        var value = GetValueString(filter.Value, type);
                        filters.Add(string.Format(template, filter.Field, value));
                    }

                    if (filter.Filters.Any())
                    {
                        var childFilters = BuildQueryString(filter.Filters, isAnd);
                        if (!string.IsNullOrWhiteSpace(childFilters))
                            filters.Add($"({childFilters})");
                    }
                }
                else if (item is AndList<ODataFilter> andList)
                {
                    filters.Add($"({BuildQueryString(andList, true)})");
                }
                else if (item is OrList<ODataFilter> orList)
                {
                    filters.Add($"({BuildQueryString(orList, false)})");
                }
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
                        var type = FieldType.Identify(item.GetType());
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
                    valString = ((DateTime)value!).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
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
            
            return valString;
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

    internal class AndList<T> : List<T>
    { }

    internal class OrList<T> : List<T>
    { }
}
