using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components.App;
using ShiftSoftware.ShiftBlazor.Layouts;
using ViewerMainLayout = ShiftBlazor.Tests.Viewer.MainLayout;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.App;

/// <summary>
/// The not-found page as <see cref="DefaultApp"/> shows it. The router renders the page inside the
/// app's main layout, like every other page. That layout renders the Mud providers, and only one
/// <see cref="MudPopoverProvider"/> can exist. When the page wrapped itself in the layout a second
/// time, the second provider failed with "There is already a subscriber to the content with the
/// given section ID 'mud-overlay-to-popover-provider'", and the app showed its error UI.
/// </summary>
public class NotFoundTests : ShiftBlazorTestContext
{
    public NotFoundTests()
    {
        // A later registration overrides an earlier one, so the components and services get this
        // runtime instead of bUnit's own.
        Services.AddSingleton<IJSRuntime>(new ValueReadingJSRuntime((IJSInProcessRuntime)JSInterop.JSRuntime));
    }

    // "/no-page-here" has no page, so the router shows its NotFoundPage. Logout sends the user to
    // such a URL when the app has no page at "/". "/not-found" is the not-found page's own route.
    // A null layout keeps the default ShiftMainLayout. The Viewer's layout stands for an app's own
    // layout (MainLayout="@typeof(...)"), which renders its own Mud providers.
    [Theory]
    [InlineData("/no-page-here", null)]
    [InlineData("/no-page-here", typeof(ViewerMainLayout))]
    [InlineData("/not-found", null)]
    [InlineData("/not-found", typeof(ViewerMainLayout))]
    public void RendersThePageInsideOneLayout(string url, Type? mainLayout)
    {
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo(url);

        // The test assembly has no pages. ShiftBlazor's assembly makes "/not-found" an ordinary
        // route match, and every other URL has no match.
        var cut = Render<DefaultApp>(parameters => parameters
            .Add(p => p.AppAssembly, typeof(NotFoundTests).Assembly)
            .Add(p => p.AdditionalAssemblies, new[] { typeof(NotFound).Assembly })
            .Add(p => p.MainLayout, mainLayout));

        cut.WaitForAssertion(() => Assert.Equal("404", cut.FindComponent<NotFound>().Find("h1").TextContent));

        var layout = Assert.Single(cut.FindComponents<LayoutComponentBase>());
        Assert.IsType(mainLayout ?? typeof(ShiftMainLayout), layout.Instance);
        Assert.True(layout.HasComponent<NotFound>());
        Assert.Single(cut.FindComponents<MudPopoverProvider>());
        Assert.False(Renderer.UnhandledException.IsCompleted);
    }

    /// <summary>
    /// bUnit's JS runtime does not implement <see cref="IJSRuntime.GetValueAsync{TValue}(string)"/>,
    /// which .NET 10 added, so a call to it throws NotImplementedException. <see cref="LayoutBase"/>
    /// makes this call after its first render, when ShiftModal reads window.location.href. This
    /// runtime answers a read with the default value, the same way bUnit's loose mode answers an
    /// invocation that no test has set up. It passes every other call to bUnit.
    /// </summary>
    private sealed class ValueReadingJSRuntime(IJSInProcessRuntime bunitRuntime) : IJSInProcessRuntime
    {
        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
            => ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken)
            => ValueTask.FromResult<TValue>(default!);

        public TResult Invoke<TResult>(string identifier, params object?[]? args)
            => bunitRuntime.Invoke<TResult>(identifier, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => bunitRuntime.InvokeAsync<TValue>(identifier, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => bunitRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }
}
