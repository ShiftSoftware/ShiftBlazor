using Microsoft.AspNetCore.Components;
using Microsoft.OData.Client;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ODataParameters<TEntity> where TEntity : ShiftEntityDTOBase
    {
        public ODataParameters(string entitySetName, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(entitySetName))
            {
                throw new ArgumentNullException(nameof(EntitySetName));
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentNullException(nameof(BaseUrl));
            }

            EntitySetName = entitySetName;
            BaseUrl = baseUrl;
        }

        public string EntitySetName;
        public string BaseUrl;
        public string? DataValueField;
        public string? DataTextField;

        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        public ODataQuery ODataQuery;
        public DataServiceQuery<TEntity> QueryBuilder => ODataQuery.CreateQuery<TEntity>(EntitySetName);
    }
}
