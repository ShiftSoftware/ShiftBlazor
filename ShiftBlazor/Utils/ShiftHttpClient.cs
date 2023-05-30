using ShiftSoftware.ShiftBlazor.Events;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class CustomMessageHandler : DelegatingHandler
    {
        public CustomMessageHandler()
        {
            //add this to solve "The inner handler has not been assigned"
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            EventComponentBase.TriggerRequestStarted(request.RequestUri);

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.StatusCode >= System.Net.HttpStatusCode.BadRequest)
                {
                    EventComponentBase.TriggerRequestFailed(request.RequestUri);
                }
                return response;
            }
            catch (Exception)
            {
                EventComponentBase.TriggerRequestFailed(request.RequestUri);
                throw;
            }
        }
    }
}
