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
        public Uri? FailedUrl { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Query = request.RequestUri?.Query;

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                return response;
            }
            catch (Exception)
            {
                FailedUrl = request.RequestUri;
                throw;
            }
        }
    }
}
