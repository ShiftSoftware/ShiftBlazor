using Bunit.TestDoubles;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ShiftBlazor.Tests;

public class ShiftBlazorTestContext : TestContext
{
    public static string BaseUrl = "http://localhost";
    public static string ODataBaseUrl = "/odata";
    public static string ApiBaseUrl = "/api";

    public List<SampleDTO> Values = new();

    public ShiftBlazorTestContext()
    {
        for (var i = 0; i < 100; i++ )
        {
            Values.Add(new SampleDTO { Name = "Sample " + i, ID = i.ToString() });
        }

        Values[3].IsDeleted = true;

        var mock = Services.AddMockHttpClient();
        mock.When(ODataBaseUrl + "/Product").RespondJson(new ODataResult<SampleDTO>
        {
            value = Values
        });
        mock.When(HttpMethod.Get, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Post, ApiBaseUrl.AddUrlPath("Product")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Put, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Delete, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First(x => x.IsDeleted == true) });

        Services.AddShiftBlazor(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = BaseUrl;
                options.ApiPath = ApiBaseUrl;
                options.ODataPath = ODataBaseUrl;
                options.UserListEndpoint = "/odata/PublicUser";
                options.AddLanguage("en-US", "EN")
                       .AddLanguage("es-US", "EN")
                       .AddLanguage("ar-AE", "EN")
                       .AddLanguage("en-GB", "EN");
            };
            config.MudBlazorConfiguration = options =>
            {
                options.SnackbarConfiguration.ShowTransitionDuration = 0;
                options.SnackbarConfiguration.HideTransitionDuration = 0;
            };
        });
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddTypeAuth(o => { });

        this.AddTestAuthorization();
    }
}