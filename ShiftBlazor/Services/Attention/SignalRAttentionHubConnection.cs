using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftIdentity.Blazor;

namespace ShiftSoftware.ShiftBlazor.Services;

/// <summary>
/// Production <see cref="IAttentionHubConnection"/> wrapping a real SignalR
/// <see cref="HubConnection"/> with automatic reconnect and bearer-token auth (the token is
/// pulled from <see cref="IIdentityStore"/> per connect/reconnect, so it stays current).
/// </summary>
internal sealed class SignalRAttentionHubConnection : IAttentionHubConnection
{
    private readonly HubConnection connection;

    public SignalRAttentionHubConnection(string hubUrl, IServiceProvider services)
    {
        connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // IIdentityStore may be absent in hosts without ShiftIdentity wired — the hub
                // then connects unauthenticated and the server's [Authorize] rejects it, which
                // is the correct outcome (no token → no real-time, same as any secured call).
                var identityStore = services.GetService<IIdentityStore>();
                if (identityStore is not null)
                    options.AccessTokenProvider = async () => (await identityStore.GetTokenAsync())?.Token;
            })
            .WithAutomaticReconnect()
            .Build();
    }

    public string? ConnectionId => connection.ConnectionId;

    public void OnAttentionRaised(Func<AttentionRealtimePayload, Task> handler) =>
        connection.On(AttentionRealtime.MessageName, handler);

    public void OnReconnected(Func<Task> handler) =>
        connection.Reconnected += _ => handler();

    public Task StartAsync() => connection.StartAsync();

    public Task SendAsync(string method, string entityType) => connection.SendAsync(method, entityType);

    public Task SendAsync(string method, string entityType, string entityId, string? scope) =>
        connection.SendAsync(method, entityType, entityId, scope);

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}

/// <summary>Builds <see cref="SignalRAttentionHubConnection"/>s; the default registration in <c>AddShiftBlazor</c>.</summary>
internal sealed class SignalRAttentionHubConnectionFactory : IAttentionHubConnectionFactory
{
    private readonly IServiceProvider services;

    public SignalRAttentionHubConnectionFactory(IServiceProvider services) => this.services = services;

    public IAttentionHubConnection Create(string hubUrl) => new SignalRAttentionHubConnection(hubUrl, services);
}
