using MudBlazor;

namespace System.Collections.Generic
{
    public static class FilterDefinitionExtension
    {
        public static IEnumerable<string> ToODataFilter<T>(this ICollection<IFilterDefinition<T>> filterDefinitions)
        {
            return filterDefinitions
                .Where(IsValidFilter)
                .Select(GetFilterString)
                .Distinct();
        }

        private static bool IsValidFilter<T>(IFilterDefinition<T> filterDefinition)
        {
            return IsValidFilter(filterDefinition.Value, filterDefinition.Operator);
        }

        private static bool IsValidFilter(object? value, string? filterOperator)
        {
            return !(value == null && filterOperator != FilterOperator.String.Empty && filterOperator != FilterOperator.String.NotEmpty);
        }

        private static string GetFilterString<T>(IFilterDefinition<T> definition)
        {
            var field = definition.Column!.PropertyName;
            return GetFilterString(field, definition.Operator!, definition.Value, definition.FieldType);
        }

        private static string GetFilterString(string field, string op, object? value, FieldType? fieldType = null)
        {
            var _fieldType = fieldType ?? FieldType.Identify(value?.GetType());
            var _value = GetValue(value, _fieldType);
            var filterTemplate = CreateFilterTemplate(op);
            return string.Format(filterTemplate, field, _value);
        }

        private static object? GetValue(object? value, FieldType fieldType)
        {
            if (value != null)
            {
                if (fieldType.IsString)
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
            }

            return value;
        }

        private static string CreateFilterTemplate(string? filterOperator)
        {
            var filterTemplate = string.Empty;
            switch (filterOperator)
            {
                case FilterOperator.Number.Equal:
                case FilterOperator.String.Equal:
                case FilterOperator.DateTime.Is:
                    filterTemplate = "{0} eq {1}";
                    break;
                case FilterOperator.Number.NotEqual:
                case FilterOperator.String.NotEqual:
                case FilterOperator.DateTime.IsNot:
                    filterTemplate = "{0} ne {1}";
                    break;
                case FilterOperator.Number.GreaterThan:
                case FilterOperator.DateTime.After:
                    filterTemplate = "{0} gt {1}";
                    break;
                case FilterOperator.Number.GreaterThanOrEqual:
                case FilterOperator.DateTime.OnOrAfter:
                    filterTemplate = "{0} ge {1}";
                    break;
                case FilterOperator.Number.LessThan:
                case FilterOperator.DateTime.Before:
                    filterTemplate = "{0} lt {1}";
                    break;
                case FilterOperator.Number.LessThanOrEqual:
                case FilterOperator.DateTime.OnOrBefore:
                    filterTemplate = "{0} le {1}";
                    break;
                case FilterOperator.String.Contains:
                    filterTemplate = "contains({0},{1})";
                    break;
                case FilterOperator.String.NotContains:
                    filterTemplate = "not contains({0},{1})";
                    break;
                case FilterOperator.String.StartsWith:
                    filterTemplate = "startswith({0},{1})";
                    break;
                case FilterOperator.String.EndsWith:
                    filterTemplate = "endswith({0},{1})";
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
