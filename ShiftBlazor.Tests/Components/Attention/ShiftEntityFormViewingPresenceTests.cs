using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Attention;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Attention;

/// <summary>
/// Covers <see cref="ShiftEntityForm{T}"/>'s <c>ReportViewingPresence</c> behavior against a
/// recording fake hub client: a report starts when an existing record is shown (with the
/// optional scope), no report happens in create mode or without the opt-in, the report stops
/// when the form is disposed, and a Key change stops the old report and starts a new one.
/// </summary>
public class ShiftEntityFormViewingPresenceTests : ShiftBlazorTestContext
{
    private readonly RecordingAttentionHubClient _hubClient = new();

    public ShiftEntityFormViewingPresenceTests()
    {
        // A later registration overrides an earlier one: the fake replaces the SignalR-backed
        // hub client that AddShiftBlazor registered.
        Services.AddScoped<IAttentionHubClient>(_ => _hubClient);
    }

    /// <summary>
    /// Renders the form together with a <see cref="MudPopoverProvider"/> sibling. The form's
    /// toolbar uses popover-based MudBlazor components, which require that provider somewhere
    /// in the render tree.
    /// </summary>
    private IRenderedComponent<ShiftEntityForm<SampleDTO>> RenderForm(string? key, bool report = true, string? scope = null)
    {
        var root = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<ShiftEntityForm<SampleDTO>>(1);
            builder.AddComponentParameter(2, nameof(ShiftEntityForm<SampleDTO>.Endpoint), "Product");
            builder.AddComponentParameter(3, nameof(ShiftEntityForm<SampleDTO>.ReportViewingPresence), report);
            builder.AddComponentParameter(4, nameof(ShiftEntityForm<SampleDTO>.ViewingPresenceScope), scope);
            if (key is not null)
                builder.AddComponentParameter(5, nameof(ShiftEntityForm<SampleDTO>.Key), (object)key);
            builder.CloseComponent();
        });

        return root.FindComponent<ShiftEntityForm<SampleDTO>>();
    }

    [Fact]
    public void ReportsViewing_WhenAnExistingRecordIsShown()
    {
        var cut = RenderForm(key: "1");

        cut.WaitForAssertion(() =>
            Assert.Equal((BaseUrl, "Product", "1", null), Assert.Single(_hubClient.Starts)));
    }

    [Fact]
    public void ReportsViewing_WithTheConfiguredScope()
    {
        var cut = RenderForm(key: "1", scope: "Details");

        cut.WaitForAssertion(() =>
            Assert.Equal((BaseUrl, "Product", "1", "Details"), Assert.Single(_hubClient.Starts)));
    }

    [Fact]
    public void DoesNotReport_InCreateMode()
    {
        var cut = RenderForm(key: null);

        // The create-mode form rendered fully; no report may have been made.
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("form")));
        Assert.Empty(_hubClient.Starts);
    }

    [Fact]
    public void DoesNotReport_WithoutTheOptIn()
    {
        var cut = RenderForm(key: "1", report: false);

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("form")));
        Assert.Empty(_hubClient.Starts);
    }

    [Fact]
    public void StopsReporting_WhenTheFormIsDisposed()
    {
        var cut = RenderForm(key: "1");
        cut.WaitForAssertion(() => Assert.Single(_hubClient.Starts));

        DisposeComponents();

        Assert.Equal(("Product", "1", null), Assert.Single(_hubClient.Stops));
    }

    [Fact]
    public void RestartsReporting_WhenTheKeyChanges()
    {
        var cut = RenderForm(key: "1");
        cut.WaitForAssertion(() => Assert.Single(_hubClient.Starts));

        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Key, "2"));

        cut.WaitForAssertion(() =>
        {
            // The old record's report ended and the new record's report started.
            Assert.Equal(("Product", "1", null), Assert.Single(_hubClient.Stops));
            Assert.Contains((BaseUrl, "Product", "2", null), _hubClient.Starts);
        });
    }

    // ---- Fake hub client ----

    private sealed class RecordingAttentionHubClient : IAttentionHubClient
    {
        public List<(string BaseAddress, string EntityType, string EntityId, string? Scope)> Starts { get; } = new();
        public List<(string EntityType, string EntityId, string? Scope)> Stops { get; } = new();

        public Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised)
            => Task.FromResult<IAsyncDisposable>(new Handle(() => { }));

        public string? GetConnectionId(string apiBaseAddress) => null;

        public Task<IAsyncDisposable> StartViewingAsync(string apiBaseAddress, string entityType, string entityId, string? scope = null)
        {
            Starts.Add((apiBaseAddress, entityType, entityId, scope));
            return Task.FromResult<IAsyncDisposable>(new Handle(() => Stops.Add((entityType, entityId, scope))));
        }

        private sealed class Handle : IAsyncDisposable
        {
            private readonly Action onDispose;
            public Handle(Action onDispose) => this.onDispose = onDispose;
            public ValueTask DisposeAsync() { onDispose(); return ValueTask.CompletedTask; }
        }
    }
}
