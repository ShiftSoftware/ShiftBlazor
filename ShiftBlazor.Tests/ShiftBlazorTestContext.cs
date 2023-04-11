using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Tests;

public class ShiftBlazorTestContext : TestContext
{
    public static string BaseUrl = "http://localhost";
    public static string ODataBaseUrl = "/odata";
    public static string ApiBaseUrl = "/api";

    public List<Sample> Values = new()
    {
        new Sample { Name = "Sample 1", ID = 1 },
        new Sample { Name = "Sample 2", ID = 2 },
        new Sample { Name = "Sample 3", ID = 3, IsDeleted = true },
    };

    public ShiftBlazorTestContext()
    {
        var mock = Services.AddMockHttpClient();
        mock.When(ODataBaseUrl + "/Product").RespondJson(new ODataResult<Sample>
        {
            value = Values
        });
        mock.When(HttpMethod.Get, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<Sample> { Entity = Values.First() });
        mock.When(HttpMethod.Post, ApiBaseUrl.AddUrlPath("Product")).RespondJson(new ShiftEntityResponse<Sample> { Entity = Values.First() });
        mock.When(HttpMethod.Put, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<Sample> { Entity = Values.First() });
        mock.When(HttpMethod.Delete, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<Sample> { Entity = Values.First(x => x.IsDeleted == true) });

        Services.AddShiftServices(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = BaseUrl;
                options.ApiPath = ApiBaseUrl;
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