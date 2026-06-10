using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftBlazor.Services;

/// <summary>
/// Default <see cref="IAttentionHubClient"/>. Maintains one <see cref="IAttentionHubConnection"/>
/// per hub URL, with reference-counted per-entity-type group subscriptions and dispatch to the
/// registered handlers. Registered scoped by <c>AddShiftBlazor</c> (one connection per WASM app
/// / per Server circuit).
/// </summary>
internal sealed class AttentionHubClient : IAttentionHubClient, IAsyncDisposable
{
    private readonly IAttentionHubConnectionFactory connectionFactory;
    private readonly object gate = new();
    private readonly Dictionary<string, ConnectionEntry> connections = new(StringComparer.Ordinal);

    public AttentionHubClient(IAttentionHubConnectionFactory connectionFactory)
        => this.connectionFactory = connectionFactory;

    public Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised)
    {
        var hubUrl = BuildHubUrl(apiBaseAddress);

        ConnectionEntry entry;
        lock (gate)
        {
            if (!connections.TryGetValue(hubUrl, out entry!))
            {
                entry = new ConnectionEntry(connectionFactory.Create(hubUrl));
                connections[hubUrl] = entry;
            }
        }

        return entry.AddAsync(entityType, onRaised);
    }

    public string? GetConnectionId(string apiBaseAddress)
    {
        var hubUrl = BuildHubUrl(apiBaseAddress);
        lock (gate)
            return connections.TryGetValue(hubUrl, out var entry) ? entry.ConnectionId : null;
    }

    /// <summary>
    /// Resolves the hub URL from the API base address. The hub route
    /// (<see cref="AttentionRealtime.DefaultHubRoute"/>) is rooted, so it resolves against the
    /// base's authority — e.g. <c>https://host/api</c> + <c>/hubs/attention</c> →
    /// <c>https://host/hubs/attention</c>, matching <c>app.MapAttentionHub()</c> at the app root.
    /// </summary>
    private static string BuildHubUrl(string apiBaseAddress)
        => new Uri(new Uri(apiBaseAddress, UriKind.Absolute), AttentionRealtime.DefaultHubRoute).ToString();

    public async ValueTask DisposeAsync()
    {
        List<ConnectionEntry> all;
        lock (gate)
        {
            all = connections.Values.ToList();
            connections.Clear();
        }

        foreach (var entry in all)
            await entry.DisposeAsync();
    }

    /// <summary>One connection plus its per-entity-type handler lists.</summary>
    private sealed class ConnectionEntry : IAsyncDisposable
    {
        private readonly IAttentionHubConnection connection;
        private readonly object gate = new();
        private readonly Dictionary<string, List<Func<AttentionRealtimePayload, Task>>> handlers = new(StringComparer.Ordinal);
        private Task? startTask;

        public ConnectionEntry(IAttentionHubConnection connection)
        {
            this.connection = connection;
            connection.OnAttentionRaised(DispatchAsync);
            // Groups are dropped when SignalR transparently reconnects — re-join them all.
            connection.OnReconnected(ResubscribeAllAsync);
        }

        public string? ConnectionId => connection.ConnectionId;

        public async Task<IAsyncDisposable> AddAsync(string entityType, Func<AttentionRealtimePayload, Task> handler)
        {
            bool isNewGroup;
            lock (gate)
            {
                if (!handlers.TryGetValue(entityType, out var list))
                {
                    list = new List<Func<AttentionRealtimePayload, Task>>();
                    handlers[entityType] = list;
                }
                isNewGroup = list.Count == 0;
                list.Add(handler);
            }

            await EnsureStartedAsync();

            if (isNewGroup)
                await TrySendAsync("SubscribeToEntityType", entityType);

            return new Subscription(this, entityType, handler);
        }

        private async Task RemoveAsync(string entityType, Func<AttentionRealtimePayload, Task> handler)
        {
            bool groupEmpty;
            lock (gate)
            {
                if (!handlers.TryGetValue(entityType, out var list))
                    return;

                list.Remove(handler);
                groupEmpty = list.Count == 0;
                if (groupEmpty)
                    handlers.Remove(entityType);
            }

            if (groupEmpty)
                await TrySendAsync("UnsubscribeFromEntityType", entityType);
        }

        private Task DispatchAsync(AttentionRealtimePayload payload)
        {
            Func<AttentionRealtimePayload, Task>[] targets;
            lock (gate)
                targets = handlers.TryGetValue(payload.EntityType, out var list)
                    ? list.ToArray()
                    : Array.Empty<Func<AttentionRealtimePayload, Task>>();

            // One component's handler throwing must not starve the others.
            return Task.WhenAll(targets.Select(async h =>
            {
                try { await h(payload); } catch { /* component-side failure, isolated */ }
            }));
        }

        private async Task ResubscribeAllAsync()
        {
            string[] entityTypes;
            lock (gate)
                entityTypes = handlers.Keys.ToArray();

            foreach (var entityType in entityTypes)
                await TrySendAsync("SubscribeToEntityType", entityType);
        }

        private Task EnsureStartedAsync()
        {
            lock (gate)
                startTask ??= connection.StartAsync();
            return startTask;
        }

        private async Task TrySendAsync(string method, string entityType)
        {
            // A send can fail if the connection is mid-(re)connect; the next Reconnected fires
            // ResubscribeAll, so a dropped Subscribe is self-healing. Best-effort, like the hint itself.
            try { await connection.SendAsync(method, entityType); } catch { }
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();

        private sealed class Subscription : IAsyncDisposable
        {
            private readonly ConnectionEntry entry;
            private readonly string entityType;
            private readonly Func<AttentionRealtimePayload, Task> handler;
            private bool disposed;

            public Subscription(ConnectionEntry entry, string entityType, Func<AttentionRealtimePayload, Task> handler)
            {
                this.entry = entry;
                this.entityType = entityType;
                this.handler = handler;
            }

            public async ValueTask DisposeAsync()
            {
                if (disposed) return;
                disposed = true;
                await entry.RemoveAsync(entityType, handler);
            }
        }
    }
}
