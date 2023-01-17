namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class CustomMessageHandler : DelegatingHandler
    {
        public string? Query { get; set; }

        public CustomMessageHandler()
        {
            //add this to solve "The inner handler has not been assigned"
            InnerHandler = new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Query = request.RequestUri?.Query;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
