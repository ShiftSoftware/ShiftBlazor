using Microsoft.OData.Client;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class ODataQuery : DataServiceContext
    {
        public ODataQuery(string serviceRoot) :
                this(new Uri(serviceRoot), ODataProtocolVersion.V4)
        {
        }

        public ODataQuery(Uri serviceRoot, ODataProtocolVersion protocolVersion) :
                base(serviceRoot, protocolVersion)
        {
        }

        override public DataServiceQuery<T> CreateQuery<T>(string entitytSetName)
        {
            return base.CreateQuery<T>(entitytSetName);
        }
    }
}
