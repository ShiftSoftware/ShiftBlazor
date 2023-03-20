using RichardSzalay.MockHttp;

namespace ShiftSoftware.ShiftBlazor.Tests;

public class ShiftBlazorTestContext : TestContext
{
    public static string BaseUrl = "http://localhost";
    public static string ODataBaseUrl = "/odata";

    public List<Sample> Values = new()
    {
        new Sample { Name = "Sample 1", ID = 1 },
        new Sample { Name = "Sample 2", ID = 2 },
        new Sample { Name = "Sample 3", ID = 3 }
    };

    public ShiftBlazorTestContext()
    {
        var mock = Services.AddMockHttpClient();
        mock.When(ODataBaseUrl + "/Product").RespondJson(new ODataResult<Sample>
        {
            value = Values
        });

        Services.AddShiftServices(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = BaseUrl;
                options.ApiPath = "/api";
                options.ODataPath = ODataBaseUrl;
                options.UserListEndpoint = "/odata/PublicUser";
            };
            config.MudBlazorConfiguration = options =>
            {
                options.SnackbarConfiguration.ShowTransitionDuration = 0;
                options.SnackbarConfiguration.HideTransitionDuration = 0;
            };
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}