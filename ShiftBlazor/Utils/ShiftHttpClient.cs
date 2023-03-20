namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class CustomMessageHandler : DelegatingHandler
    {
        public CustomMessageHandler()
        {
            //add this to solve "The inner handler has not been assigned"
            InnerHandler = new HttpClientHandler();
        }

        public string? Query { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Query = request.RequestUri?.Query;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
