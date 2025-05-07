using MudBlazor;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model;
using Newtonsoft.Json.Linq;
using Microsoft.OData.Client;
using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Sockets;

namespace ShiftSoftware.ShiftBlazor.Components;

public interface IForeignColumn : IODataComponent
{
    public string? DataValueField { get; set; }
    public string? ForeignTextField { get; set; }
    public string? PropertyName { get; }
    public string? Url { get; }
    public string? TEntityValueField { get; }
    public string? TEntityTextField { get; }
    public string? ForeignEntiyField { get; set; }

    internal static string GetDataValueFieldName(IForeignColumn column)
    {
        string? field = column.DataValueField;

        if (string.IsNullOrWhiteSpace(column.DataValueField) && !string.IsNullOrWhiteSpace(column.PropertyName) && !Guid.TryParse(column.PropertyName, out _))
        {
            field = Misc.GetFieldFromPropertyPath(column.PropertyName);
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            throw new Exception(message: $"'{nameof(DataValueField)}' cannot be null when 'ForeignColumn.Property' is null or is a dynamic expression");
        }

        return field;
    }

    internal static IEnumerable<string> GetForeignIds<T>(string field, List<T> items)
    {
        return items
            .Select(x => Misc.GetValueFromPropertyPath(x, field)?.ToString()!) //The ?.ToString() translates to an empty string if null is returned
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();
    }

    internal static IEnumerable<string> GetForeignIds<T>(IForeignColumn column, List<T> items)
    {
        var field = GetDataValueFieldName(column);
        return GetForeignIds(field, items);
    }

    public static DataServiceQuery<TEntity> GetODataUrl<TEntity>(IForeignColumn column, ODataQuery? oDataQuery = null)
        where TEntity : ShiftEntityDTOBase
    {
        return oDataQuery.CreateNewQuery<TEntity>(column.EntitySet, column.Url);
    }

    public static async Task<IEnumerable<T>?> GetForeignColumnValues<T>(IForeignColumn column, IEnumerable<string> itemIds, ODataQuery oDataQuery, HttpClient httpClient)
    {
        return (IEnumerable<T>?) await _GetForeignColumnValues(column, itemIds, oDataQuery, httpClient);
    }

    public static async Task<IEnumerable<object>?> GetForeignColumnValues(IForeignColumn column, IEnumerable<string> itemIds, ODataQuery oDataQuery, HttpClient httpClient)
    {
        return (IEnumerable<object>?) await _GetForeignColumnValues(column, itemIds, oDataQuery, httpClient);
    }

    private static async Task<object?> _GetForeignColumnValues(IForeignColumn column, IEnumerable<string> itemIds, ODataQuery oDataQuery, HttpClient httpClient)
    {
        if (itemIds.Any())
        {
            try
            {
                var TEntity = column.GetType().GetGenericArguments().Last(); 
                Type oDataType = typeof(ODataDTO<>).MakeGenericType(TEntity);

                var query = GetODataUrl<ShiftEntityDTOBase>(column, oDataQuery);

                if (column.ForeignEntiyField is null)
                {
                    query = query.AddQueryOption("$select", $"{column.TEntityValueField},{column.TEntityTextField}");
                }

                var url = query
                    .WhereQuery(x => itemIds.Contains(x.ID))
                    .ToString();

                var res = await httpClient.GetAsync(url);

                //Type te = typeof(ODataDTO<>);

                if (res.IsSuccessStatusCode)
                {
                    var result = await res.Content.ReadFromJsonAsync(oDataType);

                    if (result != null)
                    {
                        return result.GetType().GetProperty(nameof(ODataDTO<object>.Value))?.GetValue(result);
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        return null;
    } 
}
