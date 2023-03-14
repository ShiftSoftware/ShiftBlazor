using Microsoft.OData.Client;
using ShiftSoftware.ShiftBlazor.Extensions;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ODataQuery : DataServiceContext
    {
        public ODataQuery(SettingManager settings) :
            this(settings.Configuration.BaseAddress.AddUrlPath(settings.Configuration.ODataPath))
        {
        }

        public ODataQuery(string serviceRoot) :
                this(new Uri(serviceRoot), ODataProtocolVersion.V4)
        {
        }

        public ODataQuery(Uri serviceRoot, ODataProtocolVersion protocolVersion) :
                base(serviceRoot, protocolVersion)
        {
        }
    }
}
