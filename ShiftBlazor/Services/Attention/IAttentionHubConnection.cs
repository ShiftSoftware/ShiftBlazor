using System;
using System.Threading.Tasks;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftBlazor.Services;

/// <summary>
/// The slice of a SignalR connection that <see cref="AttentionHubClient"/> needs. Abstracted
/// so the client's subscription bookkeeping (ref-counting, dispatch, reconnect re-subscribe)
/// can be unit-tested against a fake, without a live hub. The production implementation wraps
/// a real <c>HubConnection</c>.
/// </summary>
internal interface IAttentionHubConnection : IAsyncDisposable
{
    /// <summary>
    /// The current connection id, or <c>null</c> before the connection has started (or while
    /// it is disconnected). Stamped on mutating requests so the server can exclude this window
    /// from the hint its own change produces.
    /// </summary>
    string? ConnectionId { get; }

    /// <summary>Registers the callback invoked for each <see cref="AttentionRealtimePayload"/> the server pushes.</summary>
    void OnAttentionRaised(Func<AttentionRealtimePayload, Task> handler);

    /// <summary>Registers a callback invoked after the connection transparently reconnects (groups must be re-joined).</summary>
    void OnReconnected(Func<Task> handler);

    /// <summary>Starts the connection. Called once.</summary>
    Task StartAsync();

    /// <summary>Invokes a hub method (<c>SubscribeToEntityType</c> / <c>UnsubscribeFromEntityType</c>) with one string argument.</summary>
    Task SendAsync(string method, string entityType);

    /// <summary>
    /// Invokes a viewing presence hub method (<c>StartViewingEntity</c> / <c>StopViewingEntity</c>)
    /// with the three arguments those methods take. A <c>null</c> <paramref name="scope"/> is
    /// normal and means the record as a whole, with no named part.
    /// </summary>
    Task SendAsync(string method, string entityType, string entityId, string? scope);
}

/// <summary>Creates an <see cref="IAttentionHubConnection"/> for a hub URL. Swapped for a fake in tests.</summary>
internal interface IAttentionHubConnectionFactory
{
    IAttentionHubConnection Create(string hubUrl);
}
