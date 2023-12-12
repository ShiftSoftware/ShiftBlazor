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

        public string? Field { get; set; }
        public ODataOperator? Operator { get; set; } = ODataOperator.Equal;

        private object? _value;
        public object? Value
        {
            get
            {
                return _value;
            }
            set
            {
                var type = FieldType.Identify(value?.GetType());
                _value = GetValue(value, type);
            }
        }

        private bool IsAnd;
        private List<object> FilterList = new List<object>();

        public ODataFilter Add(Action<ODataFilter> filterConfig)
        {
            var _filter = new ODataFilter();
            filterConfig.Invoke(_filter);
            FilterList.Add(_filter);
            return this;
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

            FilterList.Add(andFilters);
            return this;
        }

        public ODataFilter Or(params Action<ODataFilter>[] filterConfigs)
        {
            var orFilters = new OrList<ODataFilter>();
            orFilters.AddRange(GetFilters(filterConfigs));

            FilterList.Add(orFilters);
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
            return BuildQueryString(FilterList, IsAnd);
        }

        private static string BuildQueryString(dynamic filterList, bool isAnd = true)
        {
            var filters = new List<string>();
            var CombineLogic = isAnd ? "and" : "or";

            foreach (var item in filterList)
            {
                if (item is ODataFilter filter)
                {
                    var template = CreateFilterTemplate(filter.Operator);
                    filters.Add(string.Format(template, filter.Field, filter.Value));
                    if (filter.FilterList.Count > 0)
                    {
                        filters.Add($"({BuildQueryString(filter.FilterList, isAnd)})");
                    }
                }
                else if (item is AndList<ODataFilter>)
                {
                    filters.Add($"({BuildQueryString(item, true)})");
                }
                else if (item is OrList<ODataFilter>)
                {
                    filters.Add($"({BuildQueryString(item, false)})");
                }
            }

            return string.Join($" {CombineLogic} ", filters);
        }

        internal static object? GetValue(object? value, FieldType fieldType)
        {
            if (value != null)
            {

                if (value is not string && value is IEnumerable list)
                {
                    var fixedList = new List<object>();
                    foreach (var item in list)
                    {
                        var type = FieldType.Identify(item.GetType());
                        var val = GetValue(item, type);
                        if (val != null)
                            fixedList.Add(val);
                    }
                    value = string.Join(',', fixedList);
                }
                else if (fieldType.IsString)
                {
                    value = $"'{((string)value!).Replace("'", "''")}'";
                }
                else if (fieldType.IsEnum)
                {
                    value = $"'{value}'";
                }
                else if (fieldType.IsDateTime)
                {
                    value = ((DateTime)value!).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                }
                else if (fieldType.IsBoolean)
                {
                    value = value.ToString()?.ToLower();
                }
            }
            
            return value;
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
