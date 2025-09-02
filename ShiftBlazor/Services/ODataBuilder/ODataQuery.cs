using Microsoft.OData.Client;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ODataQuery : DataServiceContext
    {
        private string ODataPath;

        public ODataQuery(SettingManager settings) :
            this(settings.Configuration.BaseAddress)
        {
            ODataPath = settings.Configuration.BaseAddress;
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

        public DataServiceQuery<T> CreateNewQuery<T>(string entitySetName, string? baseUrl = null)
        {
            var builder = baseUrl == null ? this : new ODataQuery(baseUrl);
            return builder.CreateQuery<T>(entitySetName);
        }
    }
}
