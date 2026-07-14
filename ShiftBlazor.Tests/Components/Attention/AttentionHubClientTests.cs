using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Attention;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Attention;

/// <summary>
/// Covers <see cref="AttentionHubClient"/>'s subscription bookkeeping against a fake transport
/// (no live hub): one group-join per entity type regardless of how many local subscribers,
/// dispatch routed by entity type, group-leave only when the last local subscriber goes away,
/// re-join of all groups on reconnect, local echo publishing (with publisher exclusion),
/// viewing presence reports (start/stop, per-handle bookkeeping, re-send on reconnect), and
/// the hub URL resolving to the host root.
/// </summary>
public class AttentionHubClientTests
{
    private static AttentionRealtimePayload Payload(string entityType, string entityId = "1") => new()
    {
        EntityType = entityType,
        EntityId = entityId,
        Kind = AttentionRealtimeKind.Raised,
        Severity = AttentionSeverity.Info,
        RaisedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Subscribe_StartsConnectionAndJoinsGroup_OncePerEntityType()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);
        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);

        Assert.Equal(1, factory.Connection.StartCount);
        Assert.Single(factory.Connection.Sends, s => s == ("SubscribeToEntityType", "Invoice"));
    }

    [Fact]
    public async Task Subscribe_JoinsAGroupForEachDistinctEntityType()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);
        await client.SubscribeAsync("http://localhost/api", "Product", _ => Task.CompletedTask);

        Assert.Contains(("SubscribeToEntityType", "Invoice"), factory.Connection.Sends);
        Assert.Contains(("SubscribeToEntityType", "Product"), factory.Connection.Sends);
    }

    [Fact]
    public async Task Dispatch_RoutesOnlyToHandlersForThatEntityType()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var invoiceHits = new List<AttentionRealtimePayload>();
        await client.SubscribeAsync("http://localhost/api", "Invoice", p => { invoiceHits.Add(p); return Task.CompletedTask; });

        await factory.Connection.PushAsync(Payload("Invoice"));
        await factory.Connection.PushAsync(Payload("Product"));   // no subscriber → ignored

        Assert.Single(invoiceHits);
        Assert.Equal("Invoice", invoiceHits[0].EntityType);
    }

    [Fact]
    public async Task Unsubscribe_LeavesGroup_OnlyWhenLastLocalSubscriberGoes()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var sub1 = await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);
        var sub2 = await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);

        await sub1.DisposeAsync();
        Assert.DoesNotContain(("UnsubscribeFromEntityType", "Invoice"), factory.Connection.Sends);

        await sub2.DisposeAsync();
        Assert.Contains(("UnsubscribeFromEntityType", "Invoice"), factory.Connection.Sends);
    }

    [Fact]
    public async Task Reconnect_RejoinsAllActiveGroups()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);
        await client.SubscribeAsync("http://localhost/api", "Product", _ => Task.CompletedTask);

        factory.Connection.Sends.Clear();
        await factory.Connection.ReconnectAsync();

        Assert.Contains(("SubscribeToEntityType", "Invoice"), factory.Connection.Sends);
        Assert.Contains(("SubscribeToEntityType", "Product"), factory.Connection.Sends);
    }

    [Fact]
    public async Task PublishLocal_ReachesOtherHandlersOnTheSameEntityType()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var otherHits = new List<AttentionRealtimePayload>();
        await client.SubscribeAsync("http://localhost/api", "Invoice", p => { otherHits.Add(p); return Task.CompletedTask; });

        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"));

        Assert.Single(otherHits);
        Assert.Equal("Invoice", otherHits[0].EntityType);
    }

    [Fact]
    public async Task PublishLocal_SkipsTheExcludedSubscriptionsHandler()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var publisherHits = 0;
        var otherHits = 0;
        var publisherSub = await client.SubscribeAsync("http://localhost/api", "Invoice", _ => { publisherHits++; return Task.CompletedTask; });
        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => { otherHits++; return Task.CompletedTask; });

        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"), publisherSub);

        Assert.Equal(0, publisherHits);
        Assert.Equal(1, otherHits);
    }

    [Fact]
    public async Task PublishLocal_ExcludesOnlyOneOccurrenceOfASharedHandlerDelegate()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        // Two subscriptions register the SAME delegate. Excluding one of them must still let
        // the other registration receive the echo.
        var hits = 0;
        Func<AttentionRealtimePayload, Task> shared = _ => { hits++; return Task.CompletedTask; };
        var publisherSub = await client.SubscribeAsync("http://localhost/api", "Invoice", shared);
        await client.SubscribeAsync("http://localhost/api", "Invoice", shared);

        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"), publisherSub);

        Assert.Equal(1, hits);
    }

    [Fact]
    public async Task PublishLocal_DoesNotReachHandlersOfOtherEntityTypes()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var productHits = 0;
        await client.SubscribeAsync("http://localhost/api", "Product", _ => { productHits++; return Task.CompletedTask; });

        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"));

        Assert.Equal(0, productHits);
    }

    [Fact]
    public async Task PublishLocal_WithNoConnection_IsANoOp_AndCreatesNoConnection()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"));

        Assert.Empty(factory.CreatedHubUrls);
    }

    [Fact]
    public async Task PublishLocal_WithNoSubscribersForTheEntityType_IsANoOp()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var sub = await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);
        await sub.DisposeAsync();

        // Must not throw; there is no subscriber left to receive the echo.
        await client.PublishLocalAsync("http://localhost/api", Payload("Invoice"));
    }

    [Fact]
    public async Task HubUrl_ResolvesAgainstHostRoot_RegardlessOfApiPath()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);

        Assert.Equal("http://localhost/hubs/attention", Assert.Single(factory.CreatedHubUrls));
    }

    // ---- Viewing presence ----

    [Fact]
    public async Task StartViewing_StartsTheConnection_AndSendsAllThreeArguments()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.StartViewingAsync("http://localhost/api", "Invoice", "AB12", "Details");

        Assert.Equal(1, factory.Connection.StartCount);
        Assert.Equal(("StartViewingEntity", "Invoice", "AB12", "Details"), Assert.Single(factory.Connection.ViewingSends));
    }

    [Fact]
    public async Task StartViewing_WithNoScope_SendsANullScope()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.StartViewingAsync("http://localhost/api", "Invoice", "AB12");

        Assert.Equal(("StartViewingEntity", "Invoice", "AB12", null), Assert.Single(factory.Connection.ViewingSends));
    }

    [Fact]
    public async Task DisposingTheViewingHandle_SendsStop_WithTheSameArguments()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var handle = await client.StartViewingAsync("http://localhost/api", "Invoice", "AB12", "Details");
        await handle.DisposeAsync();

        Assert.Contains(("StopViewingEntity", "Invoice", "AB12", "Details"), factory.Connection.ViewingSends);
    }

    [Fact]
    public async Task DisposingTheViewingHandleTwice_SendsStopOnlyOnce()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var handle = await client.StartViewingAsync("http://localhost/api", "Invoice", "AB12");
        await handle.DisposeAsync();
        await handle.DisposeAsync();

        Assert.Single(factory.Connection.ViewingSends, s => s.Method == "StopViewingEntity");
    }

    [Fact]
    public async Task TwoComponentsViewingTwoRecords_BothStayActive_OnOneConnection()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        var handle1 = await client.StartViewingAsync("http://localhost/api", "Invoice", "1");
        await client.StartViewingAsync("http://localhost/api", "Invoice", "2");

        // Both records were reported; the second report must not replace the first one.
        Assert.Equal(1, factory.Connection.StartCount);
        Assert.Contains(("StartViewingEntity", "Invoice", "1", null), factory.Connection.ViewingSends);
        Assert.Contains(("StartViewingEntity", "Invoice", "2", null), factory.Connection.ViewingSends);

        // Ending one report must not end the other.
        await handle1.DisposeAsync();
        Assert.Contains(("StopViewingEntity", "Invoice", "1", null), factory.Connection.ViewingSends);
        Assert.DoesNotContain(("StopViewingEntity", "Invoice", "2", null), factory.Connection.ViewingSends);
    }

    [Fact]
    public async Task TwoHandlesForTheSameRecordAndScope_StopIsOnlySentWhenTheLastOneGoes()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        // Two components report the same record with the same scope. The server keeps one
        // entry per combination, so the client must not send Stop while a handle remains.
        var handle1 = await client.StartViewingAsync("http://localhost/api", "Invoice", "1");
        var handle2 = await client.StartViewingAsync("http://localhost/api", "Invoice", "1");

        await handle1.DisposeAsync();
        Assert.DoesNotContain(("StopViewingEntity", "Invoice", "1", null), factory.Connection.ViewingSends);

        await handle2.DisposeAsync();
        Assert.Contains(("StopViewingEntity", "Invoice", "1", null), factory.Connection.ViewingSends);
    }

    [Fact]
    public async Task Reconnect_ResendsEveryActiveViewingEntry()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.StartViewingAsync("http://localhost/api", "Invoice", "1");
        await client.StartViewingAsync("http://localhost/api", "Invoice", "2", "Details");
        var disposedHandle = await client.StartViewingAsync("http://localhost/api", "Product", "3");
        await disposedHandle.DisposeAsync();

        factory.Connection.ViewingSends.Clear();
        await factory.Connection.ReconnectAsync();

        // The server dropped the old connection id's entries; every active entry is re-sent,
        // and the already-stopped one is not.
        Assert.Contains(("StartViewingEntity", "Invoice", "1", null), factory.Connection.ViewingSends);
        Assert.Contains(("StartViewingEntity", "Invoice", "2", "Details"), factory.Connection.ViewingSends);
        Assert.DoesNotContain(("StartViewingEntity", "Product", "3", null), factory.Connection.ViewingSends);
    }

    [Fact]
    public async Task StartViewing_DefaultInterfaceBody_ReturnsADoNothingHandle()
    {
        // An implementation written before StartViewingAsync existed inherits the default
        // body. Calling it must not throw, and disposing the returned handle must not throw.
        IAttentionHubClient legacy = new LegacyHubClient();

        var handle = await legacy.StartViewingAsync("http://localhost/api", "Invoice", "1", "Details");

        Assert.NotNull(handle);
        await handle.DisposeAsync();
        await handle.DisposeAsync();
    }

    /// <summary>Implements only the members the earlier interface had; everything new comes from default bodies.</summary>
    private sealed class LegacyHubClient : IAttentionHubClient
    {
        public Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised)
            => throw new NotSupportedException();

        public string? GetConnectionId(string apiBaseAddress) => null;
    }

    // ---- Fake transport ----

    private sealed class FakeHubConnectionFactory : IAttentionHubConnectionFactory
    {
        public List<string> CreatedHubUrls { get; } = new();
        public FakeHubConnection Connection { get; } = new();

        public IAttentionHubConnection Create(string hubUrl)
        {
            CreatedHubUrls.Add(hubUrl);
            return Connection;
        }
    }

    private sealed class FakeHubConnection : IAttentionHubConnection
    {
        public List<(string Method, string EntityType)> Sends { get; } = new();
        public List<(string Method, string EntityType, string EntityId, string? Scope)> ViewingSends { get; } = new();
        public int StartCount { get; private set; }
        public string? ConnectionId { get; set; } = "fake-conn";
        private Func<AttentionRealtimePayload, Task>? raised;
        private Func<Task>? reconnected;

        public void OnAttentionRaised(Func<AttentionRealtimePayload, Task> handler) => raised = handler;
        public void OnReconnected(Func<Task> handler) => reconnected = handler;
        public Task StartAsync() { StartCount++; return Task.CompletedTask; }
        public Task SendAsync(string method, string entityType) { Sends.Add((method, entityType)); return Task.CompletedTask; }
        public Task SendAsync(string method, string entityType, string entityId, string? scope)
        { ViewingSends.Add((method, entityType, entityId, scope)); return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task PushAsync(AttentionRealtimePayload payload) => raised!(payload);
        public Task ReconnectAsync() => reconnected!();
    }
}
