using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Attention;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Attention;

/// <summary>
/// Covers <see cref="AttentionNavBadge"/> rendering against a mocked count endpoint and a fake
/// hub client: hidden at zero, count shown when positive, the <c>MaxCount</c> cap display, and
/// a refresh when a real-time hint (or local echo) arrives.
/// </summary>
public class AttentionNavBadgeTests : ShiftBlazorTestContext
{
    private long _count;
    private readonly FakeAttentionHubClient _hubClient = new();

    public AttentionNavBadgeTests()
    {
        // A later registration overrides an earlier one: this mock replaces the context's
        // HttpClient, and the fake replaces the SignalR-backed hub client that AddShiftBlazor
        // registered.
        var mock = Services.AddMockHttpClient();
        mock.When(BaseUrl + "/Product").RespondJson(() => new ODataDTO<JsonElement> { Count = _count, Value = new() });
        Services.AddScoped<IAttentionHubClient>(_ => _hubClient);
    }

    private IRenderedComponent<AttentionNavBadge> RenderBadge(int maxCount = 99)
        => RenderComponent<AttentionNavBadge>(parameters => parameters
            .Add(p => p.EntitySet, "Product")
            .Add(p => p.MaxCount, maxCount)
            // The periodic fallback poll is not needed here; refreshes are driven through the fake hub.
            .Add(p => p.PollInterval, TimeSpan.Zero));

    [Fact]
    public void RendersNothing_WhileCountIsZero()
    {
        _count = 0;
        var cut = RenderBadge();

        cut.WaitForAssertion(() => Assert.DoesNotContain("mud-chip", cut.Markup));
    }

    [Fact]
    public void ShowsCount_WhenRecordsNeedAttention()
    {
        _count = 5;
        var cut = RenderBadge();

        cut.WaitForAssertion(() =>
        {
            var chip = cut.Find(".mud-chip");
            Assert.Equal("5", chip.TextContent.Trim());
            Assert.Contains("needing attention", chip.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void CapsTheDisplayedCount_AtMaxCount()
    {
        _count = 250;
        var cut = RenderBadge(maxCount: 99);

        cut.WaitForAssertion(() => Assert.Equal("99+", cut.Find(".mud-chip").TextContent.Trim()));
    }

    [Fact]
    public async Task RefreshesTheCount_WhenARealtimeHintArrives()
    {
        _count = 0;
        var cut = RenderBadge();
        cut.WaitForAssertion(() => Assert.DoesNotContain("mud-chip", cut.Markup));

        // A local echo (a Cleared hint published by a form in this window) arrives through
        // the same subscription. Both kinds must update the count.
        _count = 3;
        await _hubClient.PushAsync(new AttentionRealtimePayload
        {
            EntityType = "Product",
            EntityId = "1",
            Kind = AttentionRealtimeKind.Cleared,
            Severity = AttentionSeverity.Info,
            RaisedAt = DateTimeOffset.UtcNow,
        });

        cut.WaitForAssertion(() => Assert.Equal("3", cut.Find(".mud-chip").TextContent.Trim()));
    }

    // ---- Fake hub client ----

    private sealed class FakeAttentionHubClient : IAttentionHubClient
    {
        private readonly List<Func<AttentionRealtimePayload, Task>> handlers = new();

        public Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised)
        {
            handlers.Add(onRaised);
            return Task.FromResult<IAsyncDisposable>(new Handle());
        }

        public string? GetConnectionId(string apiBaseAddress) => null;

        public Task PublishLocalAsync(string apiBaseAddress, AttentionRealtimePayload payload, IAsyncDisposable? excludeSubscription = null)
            => PushAsync(payload);

        public Task PushAsync(AttentionRealtimePayload payload)
            => Task.WhenAll(handlers.Select(h => h(payload)));

        private sealed class Handle : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
