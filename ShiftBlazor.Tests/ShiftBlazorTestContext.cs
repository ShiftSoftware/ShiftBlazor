using Bunit.TestDoubles;
using RichardSzalay.MockHttp;
using ShiftBlazor.Tests.Shared.DTOs;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

namespace ShiftSoftware.ShiftBlazor.Tests;

public class ShiftBlazorTestContext : BunitContext, IAsyncLifetime
{
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    public static string BaseUrl = "http://localhost";
    // The configuration no longer has separate API and OData paths. Components resolve
    // every URL against the single configured base address.
    public static string ODataBaseUrl = "";
    public static string ApiBaseUrl = "";

    public List<SampleDTO> Values = new();

    protected readonly MockHttpMessageHandler MockHttp;

    public ShiftBlazorTestContext()
    {
        for (var i = 0; i < 100; i++ )
        {
            Values.Add(new SampleDTO { Name = "Sample " + i, ID = i.ToString() });
        }

        Values[3].IsDeleted = true;

        var mock = Services.AddMockHttpClient();
        MockHttp = mock;

        // With no separate API/OData prefixes, the OData list and the entity endpoints share a
        // path ("/Product" is both the list and the create URL), so every mock is pinned to its
        // HTTP method and registered as an absolute URL. A relative pattern without a leading
        // slash (what AddUrlPath yields from an empty prefix) never matches in MockHttp, and an
        // any-method pattern registered first would shadow the POST/PUT ones below.
        mock.When(HttpMethod.Get, BaseUrl.AddUrlPath(ODataBaseUrl, "Users")).RespondJson(new ODataResult<User>
        {
            value = User.GenerateData(50, 50),
        });
        mock.When(HttpMethod.Get, BaseUrl.AddUrlPath(ODataBaseUrl, "Product")).RespondJson(new ODataResult<SampleDTO>
        {
            value = Values
        });
        mock.When(HttpMethod.Get, BaseUrl.AddUrlPath(ApiBaseUrl, "Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Post, BaseUrl.AddUrlPath(ApiBaseUrl, "Product")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Put, BaseUrl.AddUrlPath(ApiBaseUrl, "Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First() });
        mock.When(HttpMethod.Delete, BaseUrl.AddUrlPath(ApiBaseUrl, "Product/1")).RespondJson(new ShiftEntityResponse<SampleDTO> { Entity = Values.First(x => x.IsDeleted == true) });

        mock.When(HttpMethod.Get, BaseUrl.AddUrlPath(ApiBaseUrl, "User/1/revisions")).RespondJson(new ODataDTO<RevisionDTO>
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

        this.AddAuthorization();
    }
}
