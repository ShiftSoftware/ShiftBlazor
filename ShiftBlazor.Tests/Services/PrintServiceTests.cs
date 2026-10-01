using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Components.Print;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Tests.Services;

public class PrintServiceTests : ShiftBlazorTestContext
{
    [Theory]
    [InlineData("The report could not be generated", "The report could not be generated")]
    [InlineData(null, "Print unavailable")]
    public async Task Failed_token_response_shows_server_message_without_opening_a_tab(string? body, string expected)
    {
        var snackbars = Render<MudSnackbarProvider>();
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = JsonContent.Create(new ShiftEntityResponse
                {
                    Message = new Message("Print unavailable", body ?? string.Empty)
                })
            }));

        var error = await snackbars.InvokeAsync(() => Services.GetRequiredService<PrintService>()
            .TryPrintAsync(BaseUrl + "/orders", "7"));

        Assert.Equal(expected, error);
        snackbars.WaitForAssertion(() => Assert.Contains(expected, snackbars.Markup));
        Assert.Equal(Severity.Error, Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Severity);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, "upstream unavailable")]
    [InlineData(HttpStatusCode.BadRequest, "<html>invalid request</html>")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}")]
    [InlineData(HttpStatusCode.BadRequest, "{\"message\":\"invalid shape\"}")]
    public async Task Unstructured_error_uses_status_and_never_opens_a_tab(HttpStatusCode status, string body)
    {
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond(() => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            }));

        var error = await Services.GetRequiredService<PrintService>()
            .TryPrintAsync(BaseUrl + "/orders", "7");

        Assert.Equal($"Couldn't prepare the print (HTTP {(int)status}).", error);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Failed_preparation_remains_visible_in_the_open_print_form_and_can_be_retried()
    {
        var host = Render<IncludeMudProviders>();
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            }));
        IDialogReference dialog = default!;
        await host.InvokeAsync(async () => dialog = await Services.GetRequiredService<PrintService>()
            .OpenPrintFormAsync(BaseUrl + "/orders", "7", new PrintFormConfig { Types = [] }));
        var form = host.FindComponent<PrintForm>();

        await form.FindAll("button").Single(button => button.TextContent.Contains("Print"))
            .ClickAsync();

        Assert.Contains("HTTP 500", form.Find("[role='alert']").TextContent);
        Assert.False(dialog.Result.IsCompleted);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");

        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond("text/plain", "expires=tomorrow&token=abc");
        await form.FindAll("button").Single(button => button.TextContent.Contains("Print"))
            .ClickAsync();

        Assert.Empty(form.FindAll("[role='alert']"));
        Assert.False(dialog.Result.IsCompleted);
        Assert.Single(JSInterop.Invocations, x => x.Identifier == "open");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Successful_token_keeps_token_parameters_and_escapes_form_query_items()
    {
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("expires=tomorrow&token=abc")
            }));

        await Services.GetRequiredService<PrintService>().PrintAsync(
            BaseUrl + "/orders", "7", new Dictionary<string, string>
            {
                ["report type"] = "A&B + C", ["note"] = null!
            });

        var opened = Assert.Single(JSInterop.Invocations, x => x.Identifier == "open");
        Assert.Equal(BaseUrl + "/orders/print/7?expires=tomorrow&token=abc&report%20type=A%26B%20%2B%20C&note=",
            opened.Arguments[0]);
        Assert.Equal("_blank", opened.Arguments[1]);
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Connection_failure_shows_error_without_opening_a_tab_through_the_existing_print_call()
    {
        var snackbars = Render<MudSnackbarProvider>();
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Throw(new HttpRequestException("synthetic failure"));

        await snackbars.InvokeAsync(() => Services.GetRequiredService<PrintService>().PrintAsync(BaseUrl + "/orders", "7"));

        snackbars.WaitForAssertion(() => Assert.Contains("Couldn't prepare the print", snackbars.Find("div").TextContent));
        Assert.Equal(Severity.Error, Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Severity);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Original_constructor_shows_a_message_dialog_on_preparation_failure()
    {
        var host = Render<IncludeMudProviders>();
        var service = new PrintService(Services.GetRequiredService<HttpClient>(), JSInterop.JSRuntime,
            Services.GetRequiredService<IDialogService>());
        MockHttp.Expect(HttpMethod.Get, BaseUrl + "/orders/print-token/7")
            .Respond(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid request")
            }));

        await host.InvokeAsync(() => service.PrintAsync(BaseUrl + "/orders", "7"));

        Assert.Contains("HTTP 400", host.FindComponent<PopupMessage>().Markup);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void A_form_without_configuration_still_rejects_initialization_without_requesting_a_token()
    {
        var error = Assert.Throws<ArgumentNullException>(() => Render<PrintForm>(parameters => parameters
            .Add(p => p.Url, BaseUrl + "/orders")
            .Add(p => p.Key, "7")));

        Assert.Equal("Options", error.ParamName);
        Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "open");
    }
}
