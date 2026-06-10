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
}
