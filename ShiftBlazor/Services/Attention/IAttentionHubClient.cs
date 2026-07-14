using System;
using System.Threading.Tasks;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftBlazor.Services;

/// <summary>
/// Client-side gateway to the server's <c>AttentionHub</c>. A <c>ShiftList</c> or
/// <c>ShiftEntityForm</c> that opts into real-time updates subscribes to its entity type and
/// receives an <see cref="AttentionRealtimePayload"/> whenever a signal is raised on that type.
/// </summary>
/// <remarks>
/// Implementations share a single underlying SignalR connection per API base address across all
/// subscribers and reference-count the per-entity-type groups, so many lists/forms on a page
/// cost one connection. The connection is created lazily on the first subscription — components
/// that never opt in pay nothing. Disposing the returned handle removes that subscription (and
/// leaves the server group once its last local subscriber goes away).
/// </remarks>
public interface IAttentionHubClient
{
    /// <summary>
    /// Subscribes to real-time attention events for <paramref name="entityType"/> on the hub at
    /// <paramref name="apiBaseAddress"/>. <paramref name="onRaised"/> is invoked for each event;
    /// the caller is responsible for marshalling to its render context. Dispose the returned
    /// handle to unsubscribe.
    /// </summary>
    Task<IAsyncDisposable> SubscribeAsync(string apiBaseAddress, string entityType, Func<AttentionRealtimePayload, Task> onRaised);

    /// <summary>
    /// The current hub connection id for <paramref name="apiBaseAddress"/>, or <c>null</c> when
    /// no connection has been established to it (nothing has subscribed yet) or it hasn't finished
    /// connecting. Stamp it on a mutating request (via <c>AttentionRealtime.OriginHeader</c>) so
    /// the server excludes this window from the real-time hint the change produces — the window
    /// already reflects its own change. All lists/forms in a window share one connection per API
    /// base, so this id identifies the whole window for exclusion.
    /// </summary>
    string? GetConnectionId(string apiBaseAddress);

    /// <summary>
    /// Delivers <paramref name="payload"/> to this window's handlers that are subscribed to
    /// <c>payload.EntityType</c> on <paramref name="apiBaseAddress"/>. Handlers receive it
    /// exactly as if it had arrived from the hub. A component calls this after it has changed
    /// attention state itself. Its request carried the origin header, so the server will not
    /// send the hint back to this window. Without this local echo, another component in the
    /// same window that shows combined attention state (e.g. a navigation count badge) would
    /// never learn about the change.
    /// </summary>
    /// <remarks>
    /// Pass the caller's own subscription handle (as returned by <see cref="SubscribeAsync"/>)
    /// as <paramref name="excludeSubscription"/>. That handler is then skipped, so the publisher
    /// does not receive its own echo. This matters for forms: a form that just cleared signals
    /// keeps the cleared snapshot on screen on purpose, and its own echo would trigger a
    /// re-fetch that removes that snapshot. When no connection or no handlers exist for that
    /// base address, the call does nothing; publishing never creates a connection. The method
    /// is declared with a default body that does nothing, so implementations written against
    /// the earlier interface (consumer fakes, decorators) keep compiling; the framework client
    /// overrides it.
    /// </remarks>
    Task PublishLocalAsync(string apiBaseAddress, AttentionRealtimePayload payload, IAsyncDisposable? excludeSubscription = null)
        => Task.CompletedTask;

    /// <summary>
    /// Tells the server that this window is viewing one record right now, on the hub at
    /// <paramref name="apiBaseAddress"/>. <paramref name="entityId"/> is the hash-encoded id —
    /// the same value the form holds as its Key. <paramref name="scope"/> optionally names
    /// which part of the record is viewed (the same idea as the attention clear scope); a
    /// <c>null</c> scope means the record as a whole, with no named part. Dispose the returned
    /// handle to stop viewing.
    /// </summary>
    /// <remarks>
    /// This is presence, not a subscription: the server's viewer tracker records it so
    /// evaluators can skip raising a signal that an active viewer would acknowledge right away.
    /// It is best-effort. When a report fails or is missing, the record is treated as not
    /// viewed and signals are raised as normal. The framework client re-sends every active
    /// report after a reconnect, because the server drops all of a connection's viewer entries
    /// when that connection goes away. The method is declared with a default body that returns
    /// a do-nothing handle, so implementations written against the earlier interface (consumer
    /// fakes, decorators) keep compiling; the framework client overrides it.
    /// </remarks>
    Task<IAsyncDisposable> StartViewingAsync(string apiBaseAddress, string entityType, string entityId, string? scope = null)
        => Task.FromResult<IAsyncDisposable>(new NoOpViewingHandle());

    /// <summary>Returned by the default <see cref="StartViewingAsync"/> body. Disposing it does nothing.</summary>
    private sealed class NoOpViewingHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
