using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftBlazor.Services;

/// <summary>
/// Default <see cref="IAttentionHubClient"/>. Maintains one <see cref="IAttentionHubConnection"/>
/// per hub URL, with reference-counted per-entity-type group subscriptions, dispatch to the
/// registered handlers, and the window's active viewing presence reports. Registered scoped by
/// <c>AddShiftBlazor</c> (one connection per WASM app / per Server circuit).
/// </summary>
internal sealed class AttentionHubClient : IAttentionHubClient, IAsyncDisposable
{
    private readonly IAttentionHubConnectionFactory connectionFactory;
    private readonly object gate = new();
    private readonly Dictionary<string, ConnectionEntry> connections = new(StringComparer.Ordinal);

    public AttentionHubClient(IAttentionHubConnectionFactory connectionFactory)
        => this.connectionFactory = connectionFactory;

    public Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised)
        => GetOrCreateEntry(apiBaseAddress).AddAsync(entityType, onRaised);

    public Task<IAsyncDisposable> StartViewingAsync(string apiBaseAddress, string entityType, string entityId, string? scope = null)
        => GetOrCreateEntry(apiBaseAddress).StartViewingAsync(entityType, entityId, scope);

    /// <summary>
    /// Returns the connection entry for the base address, creating it (and with it the lazy
    /// connection) when it does not exist yet. Both subscribing and viewing use this, so the
    /// first of either starts the shared connection.
    /// </summary>
    private ConnectionEntry GetOrCreateEntry(string apiBaseAddress)
    {
        var hubUrl = BuildHubUrl(apiBaseAddress);

        lock (gate)
        {
            if (!connections.TryGetValue(hubUrl, out var entry))
            {
                entry = new ConnectionEntry(connectionFactory.Create(hubUrl));
                connections[hubUrl] = entry;
            }

            return entry;
        }
    }

    public string? GetConnectionId(string apiBaseAddress)
    {
        var hubUrl = BuildHubUrl(apiBaseAddress);
        lock (gate)
            return connections.TryGetValue(hubUrl, out var entry) ? entry.ConnectionId : null;
    }

    public Task PublishLocalAsync(string apiBaseAddress, AttentionRealtimePayload payload, IAsyncDisposable? excludeSubscription = null)
    {
        var hubUrl = BuildHubUrl(apiBaseAddress);

        // Only look up an existing entry; a publish must never create a connection. If there
        // is no entry, nothing in this window has subscribed to that base address, so there
        // is no one to send the echo to.
        ConnectionEntry? entry;
        lock (gate)
            connections.TryGetValue(hubUrl, out entry);

        return entry?.DispatchLocalAsync(payload, excludeSubscription) ?? Task.CompletedTask;
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

    /// <summary>One connection plus its per-entity-type handler lists and its active viewing reports.</summary>
    private sealed class ConnectionEntry : IAsyncDisposable
    {
        private readonly IAttentionHubConnection connection;
        private readonly object gate = new();
        private readonly Dictionary<string, List<Func<AttentionRealtimePayload, Task>>> handlers = new(StringComparer.Ordinal);
        // The active viewing reports on this connection, kept next to the handlers so a
        // reconnect can re-send them. One item per handle: two components viewing the same
        // record produce two items on purpose, so disposing one handle does not end the other
        // component's report.
        private readonly List<ViewingEntry> viewingEntries = new();
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

        /// <summary>
        /// Adds one viewing report and sends <c>StartViewingEntity</c> to the server. Like
        /// <see cref="AddAsync"/>, the first call starts the connection when it is not running
        /// yet. Dispose the returned handle to remove the report again.
        /// </summary>
        public async Task<IAsyncDisposable> StartViewingAsync(string entityType, string entityId, string? scope)
        {
            var viewing = new ViewingEntry(entityType, entityId, scope);
            lock (gate)
                viewingEntries.Add(viewing);

            await EnsureStartedAsync();
            await TrySendViewingAsync("StartViewingEntity", viewing);

            return new ViewingHandle(this, viewing);
        }

        private async Task StopViewingAsync(ViewingEntry viewing)
        {
            bool lastForTarget;
            lock (gate)
            {
                if (!viewingEntries.Remove(viewing))
                    return;

                // Another handle may still report the same (entity type, entity id, scope)
                // combination. The server keeps one entry per combination, so sending Stop now
                // would also end that other handle's report. Only send Stop when this handle
                // was the last one for the combination.
                lastForTarget = !viewingEntries.Any(v => v.SameTarget(viewing));
            }

            if (lastForTarget)
                await TrySendViewingAsync("StopViewingEntity", viewing);
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

            return InvokeIsolatedAsync(targets, payload);
        }

        /// <summary>
        /// Delivers a locally-published payload to the handlers, like <see cref="DispatchAsync"/>
        /// does. One difference: the handler that belongs to <paramref name="excludeSubscription"/>
        /// is skipped, so a publisher does not receive its own echo. The subscription handle
        /// identifies its exact handler reference, and only one occurrence of that reference is
        /// removed. This matters because two subscribers could register the same delegate; the
        /// other subscriber must still receive the echo.
        /// </summary>
        public Task DispatchLocalAsync(AttentionRealtimePayload payload, IAsyncDisposable? excludeSubscription)
        {
            var excludedHandler = excludeSubscription is Subscription sub && sub.BelongsTo(this)
                ? sub.Handler
                : null;

            Func<AttentionRealtimePayload, Task>[] targets;
            lock (gate)
            {
                if (!handlers.TryGetValue(payload.EntityType, out var list))
                    return Task.CompletedTask;

                var snapshot = new List<Func<AttentionRealtimePayload, Task>>(list);
                if (excludedHandler is not null)
                    snapshot.Remove(excludedHandler);
                targets = snapshot.ToArray();
            }

            return InvokeIsolatedAsync(targets, payload);
        }

        // If one component's handler throws, the other handlers must still run.
        private static Task InvokeIsolatedAsync(Func<AttentionRealtimePayload, Task>[] targets, AttentionRealtimePayload payload)
            => Task.WhenAll(targets.Select(async h =>
            {
                try { await h(payload); } catch { /* component-side failure, isolated */ }
            }));

        private async Task ResubscribeAllAsync()
        {
            string[] entityTypes;
            ViewingEntry[] viewing;
            lock (gate)
            {
                entityTypes = handlers.Keys.ToArray();
                viewing = viewingEntries.ToArray();
            }

            foreach (var entityType in entityTypes)
                await TrySendAsync("SubscribeToEntityType", entityType);

            // A reconnect gives the connection a new connection id, and the server drops all
            // viewer entries of the old id when it disconnects. Without this re-send, presence
            // would silently disappear after every reconnect. Sending the same combination
            // twice is harmless: the server ignores an entry that already exists.
            foreach (var entry in viewing)
                await TrySendViewingAsync("StartViewingEntity", entry);
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

        private async Task TrySendViewingAsync(string method, ViewingEntry viewing)
        {
            // Best-effort, same as TrySendAsync. A dropped StartViewingEntity is self-healing
            // too: the next Reconnected fires ResubscribeAll, which re-sends every active
            // viewing entry. A dropped StopViewingEntity only matters until the connection
            // closes; the server removes all of a connection's viewer entries on disconnect.
            try { await connection.SendAsync(method, viewing.EntityType, viewing.EntityId, viewing.Scope); } catch { }
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();

        private sealed class Subscription : IAsyncDisposable
        {
            private readonly ConnectionEntry entry;
            private readonly string entityType;
            private bool disposed;

            public Subscription(ConnectionEntry entry, string entityType, Func<AttentionRealtimePayload, Task> handler)
            {
                this.entry = entry;
                this.entityType = entityType;
                Handler = handler;
            }

            /// <summary>The handler this subscription registered. A local publish uses it to skip the publisher.</summary>
            public Func<AttentionRealtimePayload, Task> Handler { get; }

            /// <summary>Whether this subscription was created by <paramref name="entry"/>. A handle from a different connection must not exclude any handler here.</summary>
            public bool BelongsTo(ConnectionEntry entry) => ReferenceEquals(this.entry, entry);

            public async ValueTask DisposeAsync()
            {
                if (disposed) return;
                disposed = true;
                await entry.RemoveAsync(entityType, Handler);
            }
        }

        /// <summary>
        /// One active viewing report. Items are matched by object reference, so each handle
        /// removes exactly the item its own <c>StartViewingAsync</c> call added.
        /// </summary>
        private sealed class ViewingEntry
        {
            public ViewingEntry(string entityType, string entityId, string? scope)
            {
                EntityType = entityType;
                EntityId = entityId;
                Scope = scope;
            }

            public string EntityType { get; }
            public string EntityId { get; }
            public string? Scope { get; }

            /// <summary>
            /// Whether the other entry reports the same (entity type, entity id, scope)
            /// combination. Exact, case-sensitive matching — the same rule the server's viewer
            /// tracker uses.
            /// </summary>
            public bool SameTarget(ViewingEntry other) =>
                string.Equals(EntityType, other.EntityType, StringComparison.Ordinal)
                && string.Equals(EntityId, other.EntityId, StringComparison.Ordinal)
                && string.Equals(Scope, other.Scope, StringComparison.Ordinal);
        }

        private sealed class ViewingHandle : IAsyncDisposable
        {
            private readonly ConnectionEntry entry;
            private readonly ViewingEntry viewing;
            private bool disposed;

            public ViewingHandle(ConnectionEntry entry, ViewingEntry viewing)
            {
                this.entry = entry;
                this.viewing = viewing;
            }

            public async ValueTask DisposeAsync()
            {
                if (disposed) return;
                disposed = true;
                await entry.StopViewingAsync(viewing);
            }
        }
    }
}
