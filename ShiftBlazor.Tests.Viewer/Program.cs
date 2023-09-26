using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftBlazor.Tests.Viewer;
using ShiftSoftware.ShiftBlazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var baseUrl = builder.Configuration!.GetValue<string>("BaseURL")!;

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl!;
        options.ApiPath = "/api";
        options.ODataPath = "/odata";
        options.UserListEndpoint = baseUrl.AddUrlPath("odata/IdentityPublicUser"); //ToDo: this parameter should be optional.
        options.AddLanguage("en-US", "English")
               .AddLanguage("en-US", "English RTL", true);
    };
});

var app = builder.Build();
await app.RunAsync();
