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
/// re-join of all groups on reconnect, and the hub URL resolving to the host root.
/// </summary>
public class AttentionHubClientTests
{
    private static AttentionRealtimePayload Payload(string entityType, string entityId = "1") => new()
    {
        EntityType = entityType,
        EntityId = entityId,
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
    public async Task HubUrl_ResolvesAgainstHostRoot_RegardlessOfApiPath()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new AttentionHubClient(factory);

        await client.SubscribeAsync("http://localhost/api", "Invoice", _ => Task.CompletedTask);

        Assert.Equal("http://localhost/hubs/attention", Assert.Single(factory.CreatedHubUrls));
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
        public int StartCount { get; private set; }
        public string? ConnectionId { get; set; } = "fake-conn";
        private Func<AttentionRealtimePayload, Task>? raised;
        private Func<Task>? reconnected;

        public void OnAttentionRaised(Func<AttentionRealtimePayload, Task> handler) => raised = handler;
        public void OnReconnected(Func<Task> handler) => reconnected = handler;
        public Task StartAsync() { StartCount++; return Task.CompletedTask; }
        public Task SendAsync(string method, string entityType) { Sends.Add((method, entityType)); return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task PushAsync(AttentionRealtimePayload payload) => raised!(payload);
        public Task ReconnectAsync() => reconnected!();
    }
}
