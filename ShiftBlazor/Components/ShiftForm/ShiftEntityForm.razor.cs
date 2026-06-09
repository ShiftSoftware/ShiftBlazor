using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components.Print;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

[CascadingTypeParameter(nameof(T))]
public partial class ShiftEntityForm<T> : ShiftFormBasic<T>, IEntityRequestComponent<T> where T : ShiftEntityViewAndUpsertDTO, new()
{
    [Inject] public HttpClient HttpClient { get; private set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private ShiftModal ShiftModal { get; set; } = default!;
    [Inject] public SettingManager SettingManager { get; private set; } = default!;
    [Inject] public ShiftBlazorLocalizer Loc { get; private set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] PrintService PrintService { get; set; } = default!;
    [Inject] PersistentComponentState ApplicationState { get; set; } = default!;

    [Parameter] public string? BaseUrl { get; set; }
    [Parameter] public string? BaseUrlKey { get; set; }

    /// <summary>
    ///     The URL endpoint that processes the CRUD operations.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Endpoint { get; set; } = default!;

    /// <summary>
    ///     The item ID, it is also used for the CRUD operations.
    /// </summary>
    [Parameter]
    public object? Key { get; set; }

    /// <summary>
    ///     An event triggered when the state of Key has changed.
    /// </summary>
    [Parameter]
    public EventCallback<object?> KeyChanged { get; set; }

    /// <summary>
    ///     Specifies whether to render the Print button or not.
    /// </summary>
    [Parameter]
    public bool ShowPrint { get; set; }

    /// <summary>
    ///     Specifies whether to render the Delete button or not.
    /// </summary>
    [Parameter]
    public bool HideDelete { get; set; }

    /// <summary>
    ///     Specifies whether to render the Edit button or not.
    /// </summary>
    [Parameter]
    public bool HideEdit { get; set; }

    /// <summary>
    ///     Specifies whether to render the View Revisions button or not.
    /// </summary>
    [Parameter]
    public bool HideRevisions { get; set; }

    /// <summary>
    ///     Specifies whether to disable Print button or not.
    /// </summary>
    [Parameter]
    public bool DisablePrint { get; set; }

    /// <summary>
    ///     Specifies whether to disable Delete button or not.
    /// </summary>
    [Parameter]
    public bool DisableDelete { get; set; }

    /// <summary>
    ///     Specifies whether to disable Edit button or not.
    /// </summary>
    [Parameter]
    public bool DisableEdit { get; set; }

    /// <summary>
    ///     Specifies whether to disable View Revisions button or not.
    /// </summary>
    [Parameter]
    public bool DisableRevisions { get; set; }

    ///// <summary>
    /////     An event triggered when Print button is clicked, by default Print button does nothing.
    ///// </summary>
    //[Parameter]
    //public EventCallback OnPrint { get; set; }

    /// <summary>
    ///     An event triggered after getting a response from API.
    /// </summary>

    [Parameter]
    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }
    [Parameter]
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }
    [Parameter]
    public Func<Exception, ValueTask<bool>>? OnError { get; set; }
    [Parameter]
    public Func<ShiftEntityResponse<T>?, ValueTask<bool>>? OnResult { get; set; }

    [Parameter]
    public bool AllowClone { get; set; }

    [Parameter]
    public bool AllowSaveAsNew { get; set; }

    [Parameter]
    public bool ForceSaveAsNew { get; set; }

    [Parameter]
    public EventCallback<bool> ForceSaveAsNewChanged { get; set; }


    [Parameter]
    public PrintFormConfig? PrintConfig { get; set; }

    /// <summary>
    /// Attention signals for this entity (active + cleared). When active signals exist,
    /// the form renders an attention banner and a bell icon in the toolbar.
    /// </summary>
    [Parameter]
    public IReadOnlyList<StoredAttentionSignal>? AttentionSignals { get; set; }

    /// <summary>
    /// When true, the form invokes <see cref="OnAttentionCleared"/> on first load
    /// to clear active attention signals automatically.
    /// </summary>
    [Parameter]
    public bool ClearAttentionOnOpen { get; set; }

    /// <summary>
    /// Callback invoked when attention is cleared (either via ClearAttentionOnOpen
    /// or the explicit Acknowledge button on the banner).
    /// </summary>
    [Parameter]
    public EventCallback OnAttentionCleared { get; set; }

    internal bool _RenderAttentionBell;
    private bool _attentionClearFired;
    private IReadOnlyList<StoredAttentionSignal>? _internalAttentionSignals;

    /// <summary>
    /// Whether <typeparamref name="T"/> opts into the attention feature, i.e. implements
    /// <see cref="IHasAttentionSignals"/>. Computed once per closed generic type. When false the
    /// form never calls <c>GET {key}/attention</c>, so non-opted-in entities cause no request and
    /// no 404. Mirrors how <c>ShiftList</c> keys off <c>IHasAttentionSummary</c>.
    /// </summary>
    private static readonly bool _entitySupportsAttention = typeof(IHasAttentionSignals).IsAssignableFrom(typeof(T));

    /// <summary>
    /// Returns the explicit <see cref="AttentionSignals"/> parameter if provided, otherwise
    /// falls back to internally fetched signals. Explicit parameter wins so consumers can
    /// supply their own signal source.
    /// </summary>
    internal IReadOnlyList<StoredAttentionSignal>? EffectiveAttentionSignals =>
        AttentionSignals ?? _internalAttentionSignals;

    private string? _attentionLoadedForKey;

    internal string? OriginalValue { get; set; }
    internal bool Maximized { get; set; }

    internal bool _RenderCloneButton;
    internal bool _RenderPrintButton;
    internal bool _RenderRevisionButton;
    internal bool _RenderDeleteButton;
    internal bool _RenderEditButton;
    internal bool _RenderHeaderControlsDivider;
    internal bool IsTemporal = false;
    internal bool InitialRequestCompleted = false;

    internal override bool IsFooterToolbarEmpty => FooterToolbarStartTemplate == null
                                                   && FooterToolbarCenterTemplate == null
                                                   && FooterToolbarEndTemplate == null
                                                   && !_RenderSubmitButton
                                                   && Mode != FormModes.Edit
                                                   && Mode != FormModes.Archive;
    bool IsCreateMode => Mode == FormModes.Create || (Mode == FormModes.Edit && TaskInProgress == FormTasks.SaveAsNew);

    internal string ItemUrl
    {
        get
        {
            var path = IRequestComponent.GetPath(this);
            return IsCreateMode ? path : path.AddUrlPath(Key?.ToString());
        }
    }

    internal override string _SubmitText
    {
        get => SubmitText == null
            ? IsCreateMode ? Loc["CreateForm"] : Loc["SaveForm"]
            : base._SubmitText;
        set => base._SubmitText = value;
    }

    private bool ReadyToRender = false;
    public T? FetchedEntity { get; set; }
    private PersistingComponentStateSubscription persistingSubscription;

    protected override void OnInitialized()
    {
        IShortcutComponent.Register(this);

        TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();

        if (TypeAuthAction is ReadWriteDeleteAction action)
        {
            HasReadAccess = TypeAuthService?.CanRead(action) == true;
            HasWriteAccess = TypeAuthService?.CanWrite(action) == true;
            HasDeleteAccess = TypeAuthService?.CanDelete(action) == true;
        }

        OnSaveAction = SettingManager.Settings.FormOnSaveAction ?? OnSaveAction ?? DefaultAppSetting.FormOnSaveAction;

        _SubmitText = string.IsNullOrWhiteSpace(SubmitText)
            ? Loc["SubmitTextDefault"]
            : SubmitText;

    }

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new ArgumentNullException(nameof(Endpoint));
        }

        if (Key == null && Mode != FormModes.Create)
        {
            await SetMode(FormModes.Create);
        }

        if (Mode != FormModes.Create && HasReadAccess)
        {
            if (FetchedEntity == null)
            {
                await FetchItem();
            }
            else
            {
                await SetValue(FetchedEntity);
            }

            if (!ApplicationState.TryTakeFromJson<T>(
                UniqueName + nameof(FetchedEntity), out var fetchedEntity))
            {
                await FetchItem();
            }
            else
            {
                await SetValue(FetchedEntity);
            }

            persistingSubscription = ApplicationState.RegisterOnPersisting(PersistItems);
        }
        else
        {
            EditContext = new EditContext(Value);
            await OnReady.InvokeAsync();
        }

        SetTitle();
        CacheValue();

        ReadyToRender = true;
    }

    private Task PersistItems()
    {
        ApplicationState.PersistAsJson(UniqueName + nameof(FetchedEntity), FetchedEntity);
        return Task.CompletedTask;
    }

    protected override bool ShouldRender()
    {
        return ReadyToRender;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _RenderCloneButton = (SettingManager.GetFormCloneSetting() || AllowClone) && !ForceSaveAsNew;
        _RenderPrintButton = /*OnPrint.HasDelegate &&*/ ShowPrint && HasReadAccess;
        _RenderRevisionButton = !HideRevisions && HasReadAccess && IsTemporal;
        _RenderEditButton = !HideEdit && HasWriteAccess;
        _RenderDeleteButton = !HideDelete && HasDeleteAccess;

        _RenderAttentionBell = EffectiveAttentionSignals is { Count: > 0 } && HasReadAccess;
        _RenderHeaderControlsDivider = _RenderPrintButton || _RenderRevisionButton || _RenderEditButton || _RenderDeleteButton || _RenderCloneButton || _RenderAttentionBell;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (this.ForceSaveAsNew && this.Mode == FormModes.View)
        {
            this.AllowSaveAsNew = true;
            await SetMode(FormModes.Edit);
        }
    }

    public override async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {
            case KeyboardKeys.Escape:
                if (Mode == FormModes.Edit && !ForceSaveAsNew)
                {
                    await CancelChanges();
                }
                else if (Mode == FormModes.Archive)
                {
                    await CloseRevision();
                }
                else
                {
                    await Cancel();
                }
                break;

            case KeyboardKeys.KeyS:
                if (Form != null && _RenderSubmitButton)
                {
                    await SubmitHandler(this.EditContext, this.ForceSaveAsNew ? FormTasks.SaveAsNew : FormTasks.Save);
                }
                break;

            default:
                Shortcuts.TryGetValue(key, out dynamic? method);

                if (method?.HasDelegate == true)
                {
                    await method.InvokeAsync();
                }
                break;
        }
    }

    public async Task DeleteItem()
    {
        if (Mode >= FormModes.Archive && TaskInProgress != FormTasks.None)
            return;

        await RunTask(FormTasks.Delete, async () =>
        {
            var message = new Message
            {
                Title = Loc["DeleteWarningTitle"],
                Body = Loc["DeleteWarningMessage"],
            };

            var parameters = new DialogParameters
            {
                { "Message", message },
                { "Color", Color.Error },
                { "ConfirmText",  Loc["DeleteAccept"].ToString()},
                { "CancelText",  Loc["DeleteDecline"].ToString()}
            };

            var dialogReference = await DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                NoHeader = true,
                CloseOnEscapeKey = false,
            });

            var result = await dialogReference.Result;

            if (result?.Canceled != true)
            {
                try
                {
                    using var requestMessage = HttpClient.CreateRequestMessage(HttpMethod.Delete, new Uri(ItemUrl));

                    if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                        return;

                    using (var res = await HttpClient.SendAsync(requestMessage))
                    {
                        if (OnResponse != null && !(await OnResponse.Invoke(res)))
                            return;

                        await SetValue(await ParseEntityResponse(res));
                    }
                    MadeChanges = true;
                }
                catch (Exception e)
                {
                    if (OnError != null && !(await OnError.Invoke(e)))
                        return;
                    throw;
                }
            }
        });
    }

    public async Task PrintItem()
    {
        if (Mode >= FormModes.Edit && TaskInProgress != FormTasks.None)
            return;

        await RunTask(FormTasks.Print, async () =>
        {
            var path = IRequestComponent.GetPath(this);
            var id = Key?.ToString();

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (PrintConfig == null)
            {
                await PrintService.PrintAsync(path, id);
                return;
            }

            var dialogReference = await PrintService.OpenPrintFormAsync(path, id, PrintConfig);

            // wait until dialog is closed
            // the print button will keep showing loading indicator
            await dialogReference.Result;
        });
    }

    public async Task EditItem()
    {
        if (HasWriteAccess && Key != null && Mode == FormModes.View && TaskInProgress == FormTasks.None)
        {
            CacheValue();
            await SetMode(FormModes.Edit);
        }
    }

    public async Task CancelChanges()
    {
        if (TaskInProgress != FormTasks.None)
            return;

        if (await ConfirmClose())
        {
            await SetMode(FormModes.View);
            await RestoreOriginalValue();
        }
    }

    internal override async Task ValidSubmitHandler(EditContext context)
    {
        if (await OnValidSubmit.PreventableInvokeAsync(context)) return;

        var message = "";

        if (IsCreateMode)
        {
            Value.ID = null;

            message = Loc["ItemCreated"];
        }
        else
        {
            message = Loc["ItemSaved"];
        }

        T? value = null;

        try
        {
            using var request = IsCreateMode ?
                HttpClient.CreatePostRequest(Value, ItemUrl, Guid.NewGuid()) :
                HttpClient.CreatePutRequest(Value, ItemUrl);

            if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(request)))
                return;

            using (var res = await HttpClient.SendAsync(request))
            {
                if (OnResponse != null && !(await OnResponse.Invoke(res)))
                    return;

                value = await ParseEntityResponse(res);
            }
        }
        catch (Exception e)
        {
            if (OnError != null && !(await OnError.Invoke(e)))
                return;
            throw;
        }


        if (value == null)
        {
            return;
        }

        MadeChanges = true;

        if (ForceSaveAsNew)
        {
            ForceSaveAsNew = false;
            await ForceSaveAsNewChanged.InvokeAsync(false);
        }

        if (MudDialog != null && OnSaveAction == FormOnSaveAction.CloseFormOnSave)
        {
            var val = MadeChanges ? Value : null;
            ShiftModal.Close(MudDialog, val);
            IShortcutComponent.Remove(Id);
        }
        else if (OnSaveAction == FormOnSaveAction.ResetFormOnSave)
        {
            await SetValue(new T());
        }
        else if (OnSaveAction == FormOnSaveAction.ViewFormOnSave)
        {
            ShowAlert(message, Severity.Success, 5);

            if (IsCreateMode)
            {
                await UpdateUrl(value.ID);
            }
            await SetMode(FormModes.View);
            _attentionLoadedForKey = null;
            await SetValue(value);
        }

    }

    internal override async Task SetMode(FormModes mode)
    {
        var _previousMode = Mode;
        await base.SetMode(mode);
        SetTitle();

        if (AutoFocus && _previousMode <= FormModes.Archive && mode >= FormModes.Edit)
        {
            await ContentContainerRef.MudFocusFirstAsync();
        }
    }

    internal async Task SetValue(T? value, bool copyValue = true, bool isOpen = false)
    {
        if (value == null)
        {
            return;
        }

        await base.SetValue(value);

        if (copyValue)
        {
            _ = Task.Run(() =>
            {
                OriginalValue = JsonSerializer.Serialize(value);
            });
        }

        if (AttentionSignals is null && !IsCreateMode)
            await LoadAttentionSignalsInternal();

        // ClearAttentionOnOpen fires ONLY when the record is opened (isOpen) — never on the
        // post-save reload. Otherwise a save that (re-)raises a signal would clear it instantly,
        // which contradicts the property's name and hides freshly-raised attention from the user.
        var signals = EffectiveAttentionSignals;
        if (isOpen && ClearAttentionOnOpen && !_attentionClearFired &&
            signals?.Any(s => s.ClearedAt is null) == true)
        {
            _attentionClearFired = true;
            if (OnAttentionCleared.HasDelegate)
                await OnAttentionCleared.InvokeAsync();
            else
                // Auto-acknowledge: clear server-side but keep the snapshot on screen so the
                // banner stays visible this session (won't re-appear on the next open).
                await ClearAttentionInternal(reloadAfter: false);
        }
    }

    /// <summary>
    /// Fetches attention signals from <c>GET {ItemUrl}/attention</c> and stores them in
    /// <c>_internalAttentionSignals</c>. Skipped when the entity key hasn't changed since
    /// the last load, unless <paramref name="forceReload"/> is <c>true</c>.
    /// </summary>
    private async Task LoadAttentionSignalsInternal(bool forceReload = false)
    {
        // Only opted-in entities expose signals; skip the round-trip (and the resulting network
        // noise) entirely for everything else. Guards every call path into this method.
        if (!_entitySupportsAttention)
            return;

        var keyStr = Key?.ToString();
        if (string.IsNullOrEmpty(keyStr))
            return;

        if (!forceReload && keyStr == _attentionLoadedForKey)
            return;

        _attentionLoadedForKey = keyStr;

        try
        {
            var url = ItemUrl.TrimEnd('/') + "/attention";
            using var request = HttpClient.CreateRequestMessage(HttpMethod.Get, new Uri(url));
            using var res = await HttpClient.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                _internalAttentionSignals = await res.Content.ReadFromJsonAsync<List<StoredAttentionSignal>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
        }
        catch
        {
            _internalAttentionSignals = null;
        }
    }

    /// <summary>
    /// Posts to <c>POST {ItemUrl}/attention/clear</c> to clear all active signals and sets
    /// <see cref="ShiftFormBasic{T}.MadeChanges"/> so the list refreshes when the form closes.
    /// <para>
    /// When <paramref name="reloadAfter"/> is <c>true</c> (the explicit Acknowledge button) the
    /// signals are re-fetched so the banner reflects the cleared state immediately — the user
    /// deliberately dismissed it. For the auto-clear-on-open path it is <c>false</c>: the
    /// already-loaded snapshot is left in place so the banner stays visible for this session
    /// (the persisted signal is still cleared, so the next open shows nothing). Re-fetching
    /// there would empty the banner instantly and the user would never see what was flagged.
    /// </para>
    /// <para>
    /// The clear updates the entity row server-side, advancing its audit stamp — and
    /// <c>LastSaveDate</c> doubles as the optimistic-concurrency version checked on update.
    /// The endpoint returns the post-clear stamp; it is patched onto the loaded DTO (and the
    /// cancel-restore snapshot) so a subsequent edit + save doesn't trip a version conflict
    /// with the pre-clear stamp.
    /// </para>
    /// </summary>
    private async Task ClearAttentionInternal(bool reloadAfter = true)
    {
        var keyStr = Key?.ToString();
        if (string.IsNullOrEmpty(keyStr) || Endpoint is null)
            return;

        try
        {
            var url = ItemUrl.TrimEnd('/') + "/attention/clear";
            using var request = HttpClient.CreateRequestMessage(HttpMethod.Post, new Uri(url));
            using var res = await HttpClient.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                MadeChanges = true;

                await PatchConcurrencyStampFromClearResponse(res);

                if (reloadAfter)
                {
                    await LoadAttentionSignalsInternal(forceReload: true);
                    StateHasChanged();
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Applies the post-clear <c>LastSaveDate</c> from a clear response onto the loaded DTO and
    /// onto the cancel-restore snapshot (<see cref="OriginalValue"/>), keeping the form's
    /// optimistic-concurrency version current. Only <c>LastSaveDate</c> is touched — any
    /// in-progress edits on other fields are preserved (a manual acknowledge can happen in
    /// edit mode).
    /// </summary>
    private async Task PatchConcurrencyStampFromClearResponse(HttpResponseMessage res)
    {
        try
        {
            var body = await res.Content.ReadFromJsonAsync<ClearAttentionResponse>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (body?.LastSaveDate is not { } lastSaveDate)
                return;

            if (Value is not null)
                Value.LastSaveDate = lastSaveDate;

            // Without this, cancelling an edit after the clear would restore the snapshot's
            // pre-clear stamp and the next save would conflict again.
            if (!string.IsNullOrWhiteSpace(OriginalValue))
            {
                var original = JsonSerializer.Deserialize<T>(OriginalValue);
                if (original is not null)
                {
                    original.LastSaveDate = lastSaveDate;
                    OriginalValue = JsonSerializer.Serialize(original);
                }
            }
        }
        catch
        {
            // A missing/unparsable body degrades to the pre-fix behavior (stale stamp);
            // never let it break the clear flow itself.
        }
    }

    /// <summary>
    /// Called when the user clicks Acknowledge on the attention banner. Delegates to
    /// <see cref="OnAttentionCleared"/> if bound, otherwise calls <see cref="ClearAttentionInternal"/>.
    /// </summary>
    internal async Task HandleAttentionAcknowledge()
    {
        if (OnAttentionCleared.HasDelegate)
            await OnAttentionCleared.InvokeAsync();
        else
            await ClearAttentionInternal();
    }

    /// <summary>
    /// Opens the <see cref="AttentionHistoryDialog"/> showing the timeline of all signals
    /// (active + cleared) for the current entity.
    /// </summary>
    private async Task OpenAttentionHistory()
    {
        var parameters = new DialogParameters<AttentionHistoryDialog>
        {
            { x => x.Signals, EffectiveAttentionSignals },
            { x => x.EntityType, typeof(T).Name },
            { x => x.EntityId, Key?.ToString() },
        };
        await DialogService.ShowAsync<AttentionHistoryDialog>("Signal history", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
    }

    public void ResizeForm()
    {
        if (MudDialog == null)
        {
            return;
        }

        Maximized = MudDialog.Options.FullScreen != true;
        MudDialog.SetOptionsAsync(MudDialog.Options with
        {
            FullScreen = Maximized,
        });
    }

    internal async Task FetchItem(DateTimeOffset? asOf = null)
    {
        await RunTask(FormTasks.Fetch, async () =>
        {
            try
            {
                var url = asOf == null ? ItemUrl : ItemUrl + "?asOf=" + Uri.EscapeDataString((asOf.Value).ToString("O"));
                using var requestMessage = HttpClient.CreateRequestMessage(HttpMethod.Get, new Uri(url));

                if (OnBeforeRequest != null && !(await OnBeforeRequest.Invoke(requestMessage)))
                    return;

                using (var res = await HttpClient.SendAsync(requestMessage))
                {
                    if (OnResponse != null && !(await OnResponse.Invoke(res)))
                        return;

                    //res.Headers.TryGetValues(Constants.HttpHeaderVersioning, out IEnumerable<string>? versioning);
                    IsTemporal = true; //versioning?.Contains("Temporal") == true;
                    FetchedEntity = await ParseEntityResponse(res);
                    // isOpen: only a live open (asOf == null) auto-acknowledges; viewing a
                    // historical revision must not clear the current record's attention.
                    await SetValue(FetchedEntity, copyValue: asOf == null, isOpen: asOf == null);
                }
            }
            catch (Exception e)
            {
                EditContext = new EditContext(Value);
                if (OnError != null && !(await OnError.Invoke(e)))
                    return;
                throw;
            }
            finally
            {
                if (!InitialRequestCompleted)
                {
                    InitialRequestCompleted = true;
                    await OnReady.InvokeAsync();
                }
            }
            
        });
    }

    internal async Task<T?> ParseEntityResponse(HttpResponseMessage res)
    {
        if (res.StatusCode == HttpStatusCode.NoContent)
        {
            return new T();
        }

        ShiftEntityResponse<T>? result = null;

        try
        {
            result = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            if (OnResult != null && !(await OnResult.Invoke(result)))
                return null;

        }
        catch (Exception ex)
        {
            var resBody = await res.Content.ReadAsStringAsync();
            throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(resBody, ex));
        }

        if (result == null)
        {
            throw new Exception("Could not get response from server");
        }

        if (result.Message != null)
        {
            var serverSideErrors = result.Message.SubMessages
                ?.Where(x => !string.IsNullOrWhiteSpace(x.For))
                .ToDictionary(x => x.For!, x => x.SubMessages?.Select(x => x.Title).ToList());

            if (serverSideErrors != null && serverSideErrors.Count != 0)
            {
                var messageStore = shiftValidator?.MessageStore ?? new ValidationMessageStore(EditContext!);
                EditContext?.DisplayErrors(serverSideErrors!, messageStore);
                return null;
            }

            var parameters = new DialogParameters {
                { "Message", result.Message },
                { "Color", Color.Error },
                { "Icon", Icons.Material.Filled.Error },
            };

            _ = DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                NoHeader = true,
                CloseOnEscapeKey = false,
            });

            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
        }

        if (res.IsSuccessStatusCode)
        {
            var value = result.Entity;
            return value;
        }

        throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(await res.Content.ReadAsStringAsync()));
    }

    internal void SetTitle()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            return;
        }

        if (Key != null && Mode == FormModes.View)
        {
            if(Key.GetType() == typeof(string) && !string.IsNullOrWhiteSpace(Key.ToString()))
                DocumentTitle = Loc["ViewingForm", Title, Key];
            else
                DocumentTitle = Loc["ViewingForm1", Title];
        }
        else if (Key != null && Mode == FormModes.Edit)
        {
            DocumentTitle = Loc["EditingForm", Title, Key];
        }
        else if (IsCreateMode)
        {
            DocumentTitle = Loc["CreatingForm", Title];
        }
    }

    internal async Task UpdateUrl(string? key)
    {
        if (key == null || key == default)
            return;

        var oldKey = Key;

        Key = key;
        await KeyChanged.InvokeAsync(key);

        if (MudDialog == null)
        {
            if (TaskInProgress == FormTasks.Save)
                NavManager.NavigateTo(NavManager.Uri.AddUrlPath(key.ToString()));
            else if (TaskInProgress == FormTasks.SaveAsNew)
                NavManager.NavigateTo(NavManager.Uri.Replace(oldKey!.ToString()!, key.ToString()));
        }
        else
        {
            ShiftModal.UpdateKey(key);
        }
    }

    internal async Task OpenInNewTab()
    {
        var url = await JsRuntime.GetValueAsync<string>("window.location.href");

        var modals = ShiftModal.ParseModalUrl(url);
        await ShiftModal.Open(modals.First().Name, Key, ModalOpenMode.NewTab, modals.First().Parameters);
    }

    internal async Task CloneAndOpen()
    {
        if (!_RenderCloneButton) return;

        var val = JsonSerializer.Serialize(Value);
        var original = JsonSerializer.Deserialize<T>(val);
        original!.ID = null;

        var param = new Dictionary<string, object>()
        {
            ["TheItem"] = original,
        };

        var url = await JsRuntime.GetValueAsync<string>("window.location.href");

        var modals = ShiftModal.ParseModalUrl(url);
        var result = await ShiftModal.Open(modals.First().Name, parameters: param, skipQueryParamUpdate: true);

        MadeChanges = result != null && result.Canceled != true;
    }

    internal async Task ViewRevisions()
    {
        if (Mode > FormModes.Archive)
            return;

        DateTimeOffset? date = null;

        await RunTask(FormTasks.FetchRevisions, async () =>
        {
            var dParams = new DialogParameters
            {
                {"EntitySet", Endpoint.AddUrlPath(Key?.ToString(), "revisions")},
            };

            if (!string.IsNullOrWhiteSpace(Title))
            {
                dParams.Add("Title", Loc["RevisionsTitle", Title].ToString());
            }

            var options = new DialogOptions
            {
                NoHeader = true,
                CloseOnEscapeKey = false,
            };

            var dialogReference = await DialogService.ShowAsync<RevisionViewer>("", dParams, options);
            var result = await dialogReference.Result;
            date = (DateTimeOffset?)result?.Data;
        });

        if (date == null)
        {
            await CloseRevision();
        }
        else
        {
            await FetchItem(date);
            await SetMode(FormModes.Archive);
        }
    }

    internal async Task CloseRevision()
    {
        if (Mode == FormModes.Archive)
        {
            await SetMode(FormModes.View);
            await RestoreOriginalValue();
        }
    }

    private string GetTitle()
    {
        return Title ?? Loc["FormDefaultTitle"] ?? string.Empty;
    }

    internal async Task RestoreOriginalValue()
    {
        if (string.IsNullOrWhiteSpace(OriginalValue))
        {
            await SetValue(new T());
        }
        else
        {
            var original = JsonSerializer.Deserialize<T>(OriginalValue);
            await SetValue(original, false);
        }
    }

    internal void CacheValue()
    {
        OriginalValue = JsonSerializer.Serialize(Value);
    }
}
