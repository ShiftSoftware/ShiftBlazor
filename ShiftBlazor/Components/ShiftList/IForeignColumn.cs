using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public interface IForeignColumn : IODataRequest, IRequestComponent
{
    public string? DataValueField { get; set; }
    public string? ForeignTextField { get; set; }
    public string? PropertyName { get; }
    public string? Url { get; }
    public string? TEntityValueField { get; }
    public string? TEntityTextField { get; }
    public string? ForeignEntiyField { get; }

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
            .Select(x => Misc.GetValueFromPropertyPath(x!, field)?.ToString()!) //The ?.ToString() translates to an empty string if null is returned
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

    public static async Task<ODataDTO<T>?> GetForeignColumnValues<T>(IForeignColumn column, IEnumerable<string> itemIds, ODataQuery oDataQuery, HttpClient httpClient)
    {
        return (ODataDTO<T>?) await _GetForeignColumnValues(column, itemIds, oDataQuery, httpClient);
    }

    public static async Task<ODataDTO<object>?> GetForeignColumnValues(IForeignColumn column, IEnumerable<string> itemIds, ODataQuery oDataQuery, HttpClient httpClient)
    {
        return (ODataDTO<object>?) await _GetForeignColumnValues(column, itemIds, oDataQuery, httpClient);
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

                using var requestMessage = httpClient.CreateRequestMessage(HttpMethod.Get, new Uri(url));

                if (column.OnBeforeRequest != null && !(await column.OnBeforeRequest.Invoke(requestMessage)))
                    return null;

                using var res = await httpClient.SendAsync(requestMessage);

                if (column.OnResponse != null && !(await column.OnResponse.Invoke(res)))
                    return null;

                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadFromJsonAsync(oDataType);
                }

            }
            catch (Exception e)
            {
                if (column.OnError != null && !(await column.OnError.Invoke(e)))
                    return null;
                throw;
            }
        }

        return null;
    } 
}