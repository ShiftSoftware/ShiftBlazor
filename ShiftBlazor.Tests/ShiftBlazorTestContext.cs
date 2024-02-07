using Bunit.TestDoubles;
using RichardSzalay.MockHttp;
using ShiftBlazor.Tests.Shared.DTOs;
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
        mock.When(ODataBaseUrl + "/Users").RespondJson(new ODataResult<User>
        {
            value = User.GenerateData(50, 50),
        });
        mock.When(ODataBaseUrl + "/Product").RespondJson(new ODataResult<SampleDTO>
        {
            value = Values
        });
        mock.When(HttpMethod.Get, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Post, ApiBaseUrl.AddUrlPath("Product")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Put, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Delete, ApiBaseUrl.AddUrlPath("Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First(x => x.IsDeleted == true) });

        mock.When(HttpMethod.Get, ApiBaseUrl.AddUrlPath("/User/1/revisions")).RespondJson(new ODataDTO<RevisionDTO>
        {
            Value = new List<RevisionDTO> {
                new RevisionDTO {
                    ValidFrom = new DateTime(2020, 1, 1),
                    ValidTo = new DateTime(2022, 1, 1),
                    ID = "1",
                },
                new RevisionDTO {
                    ValidFrom = new DateTime(2021, 1, 1),
                    ValidTo = new DateTime(2022, 12, 1),
                    ID = "2",
                },
            }
        });

        Services.AddShiftBlazor(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = BaseUrl;
                options.ApiPath = ApiBaseUrl;
                options.ODataPath = ODataBaseUrl;
                options.UserListEndpoint = BaseUrl + "/odata/PublicUser";
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