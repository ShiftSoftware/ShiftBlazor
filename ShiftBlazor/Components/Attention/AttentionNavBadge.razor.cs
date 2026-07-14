using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>
/// Count badge for navigation menus. It shows how many records of one entity set currently
/// need attention, and it updates live. Inside a nav link it renders as a small bubble on the
/// link's icon. The bubble looks the same when the drawer is open and when it is collapsed to
/// its narrow icon-only (mini) state. Outside a nav link it renders as an inline pill. Nothing
/// is rendered while the count is zero. Updates arrive in real time through the attention hub,
/// with a periodic poll as a fallback.
/// </summary>
/// <remarks>
/// The count comes from
/// <c>GET {base}/{EntitySet}?$filter=HasActiveAttention eq true&amp;$count=true&amp;$top=0</c>.
/// The entity set's list DTO must therefore expose the attention summary columns
/// (<c>IHasAttentionSummary</c>); otherwise the filter cannot bind. The count query goes
/// through the entity's normal list authorization (TypeAuth plus data-level filters), so the
/// badge never counts records the user cannot list. The hub subscription is created once, at
/// first render, from the <see cref="EntitySet"/> and <see cref="BaseUrl"/> values of that
/// moment; the count query re-reads them on every refresh. Changing them on a live instance
/// is therefore not supported — the real-time updates would keep following the old values.
/// Use <c>@key</c> to force a new instance when they must change.
/// </remarks>
public partial class AttentionNavBadge : ComponentBase, IAsyncDisposable
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private SettingManager SettingManager { get; set; } = default!;
    [Inject] private IAttentionHubClient AttentionHubClient { get; set; } = default!;

    /// <summary>
    /// The OData entity set to count (e.g. <c>"Invoice"</c>). Its first path segment is also
    /// used as the attention hub entity type. This is the same convention <c>ShiftList</c> uses.
    /// </summary>
    [Parameter, EditorRequired]
    public string EntitySet { get; set; } = default!;

    /// <summary>
    /// Overrides the API base address for both the count query and the hub subscription.
    /// Defaults to the app's configured base address.
    /// </summary>
    [Parameter]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Interval of the periodic fallback poll. Zero or negative disables polling, leaving only
    /// the real-time hub updates. Defaults to one minute.
    /// </summary>
    [Parameter]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Cap on the displayed number; anything above renders as e.g. <c>"99+"</c>. Defaults to 99.</summary>
    [Parameter]
    public int MaxCount { get; set; } = 99;

    /// <summary>CSS classes applied to the pill.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline styles applied to the pill.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Template for the pill's <c>aria-label</c>; <c>{0}</c> is replaced with the displayed
    /// count. Defaults to an English phrase — supply a localized template in multi-language apps.
    /// </summary>
    [Parameter]
    public string? AriaLabelTemplate { get; set; }

    private long _count;
    private bool _started;
    // Set at the start of DisposeAsync. SubscribeToAttentionUpdatesAsync checks it after its
    // await, so a subscription that finishes starting on a dead badge is disposed instead of
    // stored (and leaked). The poll loop also checks it before starting.
    private bool _disposed;
    private IAsyncDisposable? _attentionSubscription;
    private readonly CancellationTokenSource _pollCancellation = new();
    // Makes the initial fetch, hub-triggered refreshes, and the fallback poll run one at a
    // time, so count fetches never overlap. Without this, an older response that finishes
    // last would overwrite a newer count.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string DisplayCount => _count > MaxCount ? $"{MaxCount}+" : _count.ToString();

    private string AriaLabel => string.Format(AriaLabelTemplate ?? "{0} records needing attention", DisplayCount);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Runs only once, on the first render (never again on prerender re-runs): one initial
        // fetch, one subscription, one poll loop.
        if (!firstRender || _started)
            return;

        _started = true;

        await RefreshCountAsync();
        await SubscribeToAttentionUpdatesAsync();

        if (!_disposed && PollInterval > TimeSpan.Zero)
            _ = PollAsync();
    }

    /// <summary>
    /// The API base address for the count query and the hub subscription: explicit
    /// <see cref="BaseUrl"/>, else the app's configured base — the same default resolution the
    /// request components use.
    /// </summary>
    private string ResolveBaseAddress()
        => BaseUrl ?? SettingManager.Configuration.BaseAddress;

    /// <summary>
    /// Re-reads the count of records with active attention. On failure, the last shown count
    /// stays. The badge is a passive indicator and must never show an error of its own.
    /// </summary>
    private async Task RefreshCountAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var url = ResolveBaseAddress().AddUrlPath(EntitySet)
                + "?$filter=" + Uri.EscapeDataString("HasActiveAttention eq true")
                + "&$count=true&$top=0";

            // $top=0 fetches no rows; only the count is returned. JsonElement is used as the
            // row type because Value stays empty.
            var result = await HttpClient.GetFromJsonAsync<ODataDTO<JsonElement>>(url, _pollCancellation.Token);

            var count = result?.Count ?? 0;
            if (count != _count)
            {
                _count = count;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch
        {
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Subscribes to the attention hub for this entity set's type. A failed connection is
    /// ignored: real-time updates are optional, and the periodic fallback poll still keeps
    /// the count updated.
    /// </summary>
    private async Task SubscribeToAttentionUpdatesAsync()
    {
        var entityType = EntitySet?.Split('/', 2)[0];
        var baseAddress = ResolveBaseAddress();
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(baseAddress))
            return;

        try
        {
            var subscription = await AttentionHubClient.SubscribeAsync(baseAddress, entityType, OnAttentionChanged);

            // The badge may have been disposed while the call above was in flight (the first
            // hub connection can take a while). Storing the subscription then would leak it:
            // DisposeAsync already ran and will not run again. Dispose the just-created
            // subscription instead of storing it.
            if (_disposed)
            {
                try { await subscription.DisposeAsync(); } catch { }
                return;
            }

            _attentionSubscription = subscription;
        }
        catch
        {
        }
    }

    /// <summary>
    /// Handles a real-time attention event. Raised and Cleared both change the count, so both
    /// trigger a refresh. Local echoes published by a component that just cleared attention
    /// arrive through this same handler. That is why a clear made in this window shows here
    /// immediately, even though the server excludes the acting window from its own broadcast.
    /// </summary>
    private Task OnAttentionChanged(AttentionRealtimePayload payload) => RefreshCountAsync();

    /// <summary>
    /// Periodic fallback poll. It covers hub outages, and it covers attention changes made by
    /// code paths that neither reach the hub group nor publish locally.
    /// </summary>
    private async Task PollAsync()
    {
        try
        {
            // The token is read once, before the loop. DisposeAsync disposes the token source,
            // and reading the Token property of a disposed source throws.
            var cancellationToken = _pollCancellation.Token;

            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await RefreshCountAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
            // The badge was disposed before the loop started; there is nothing to poll.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _pollCancellation.Cancel();
        _pollCancellation.Dispose();

        // Unsubscribe, ignoring failures. The hub client leaves the server group once its
        // last local subscriber is gone.
        try
        {
            if (_attentionSubscription is not null)
                await _attentionSubscription.DisposeAsync();
        }
        catch
        {
        }
    }
}
