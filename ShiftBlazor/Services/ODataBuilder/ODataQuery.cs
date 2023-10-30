using Microsoft.OData.Client;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ODataQuery : DataServiceContext
    {
        private string ODataPath;

        public ODataQuery(SettingManager settings) :
            this(settings.Configuration.ODataPath)
        {
            ODataPath = settings.Configuration.ODataPath;
        }

        public ODataQuery(string serviceRoot) :
                this(new Uri(serviceRoot), ODataProtocolVersion.V4)
        {
            ODataPath = serviceRoot;
        }

        public ODataQuery(Uri serviceRoot, ODataProtocolVersion protocolVersion) :
                base(serviceRoot, protocolVersion)
        {
            ODataPath = serviceRoot.AbsoluteUri;
        }

        public ODataParameters<T> CreateODataParameterObject<T>(string entitySetName, string? baseUrl = null, string? DataValueField = null, string? DataTextField = null) where T : ShiftEntityDTOBase
        {
            var url = baseUrl ?? ODataPath;

            var builder = baseUrl == null ? this : new ODataQuery(baseUrl);

            return new ODataParameters<T>(entitySetName, url)
            {
                DataValueField = DataValueField,
                DataTextField = DataTextField,
                ODataQuery = builder,
            };
        }
    }
}
