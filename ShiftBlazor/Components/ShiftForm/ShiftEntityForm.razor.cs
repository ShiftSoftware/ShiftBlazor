using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftBlazor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.TypeAuth.Core.Actions;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftEntity.Core.Extensions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftEntityForm<T> : ShiftFormBasic<T> where T : ShiftEntityViewAndUpsertDTO, new()
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [Inject] ShiftBlazorLocalizer Loc { get; set; } = default!;
        [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        ///     The URL endpoint that processes the CRUD operations.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public string Action { get; set; } = default!;

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
        public EventCallback<HttpResponseMessage> OnResponse { get; set; }

        [Parameter]
        public EventCallback<T> OnEntityResponse { get; set; }

        internal string? OriginalValue { get; set; }
        internal bool Maximized { get; set; }

        internal bool _RenderPrintButton;
        internal bool _RenderRevisionButton;
        internal bool _RenderDeleteButton;
        internal bool _RenderEditButton;
        internal bool _RenderHeaderControlsDivider;
        internal bool IsTemporal = false;

        internal Guid IdempotencyToken;

        internal string ItemUrl
        {
            get
            {
                var path = SettingManager.Configuration.ApiPath.AddUrlPath(Action);
                return Mode == FormModes.Create ? path : path.AddUrlPath(Key?.ToString());
            }
        }

        internal override string _SubmitText
        {
            get => SubmitText == null
                ? Mode == FormModes.Create ? Loc["CreateForm"] : Loc["SaveForm"]
                : base._SubmitText;
            set => base._SubmitText = value;
        }

        private bool ReadyToRender = false;

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
            if (string.IsNullOrWhiteSpace(Action))
            {
                throw new ArgumentNullException(nameof(Action));
            }

            if (Key == null && Mode != FormModes.Create)
            {
                await SetMode(FormModes.Create);
            }

            if (Mode != FormModes.Create && HasReadAccess)
            {
                await FetchItem();
            }
            else
            {
                EditContext = new EditContext(Value);
            }

            SetTitle();
            CacheValue();

            ReadyToRender = true;
        }

        protected override bool ShouldRender()
        {
            return ReadyToRender;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            _RenderPrintButton = /*OnPrint.HasDelegate &&*/ ShowPrint && HasReadAccess;
            _RenderRevisionButton = !HideRevisions && HasReadAccess && IsTemporal;
            _RenderEditButton = !HideEdit && HasWriteAccess;
            _RenderDeleteButton = !HideDelete && HasDeleteAccess;

            _RenderHeaderControlsDivider = _RenderPrintButton || _RenderRevisionButton || _RenderEditButton || _RenderDeleteButton;

            IsFooterToolbarEmpty = FooterToolbarStartTemplate == null
                && FooterToolbarCenterTemplate == null
                && FooterToolbarEndTemplate == null
                && !_RenderSubmitButton
                && Mode != FormModes.Edit
                && Mode != FormModes.Archive;
        }

        public override async ValueTask HandleShortcut(KeyboardKeys key)
        {
            switch (key)
            {
                case KeyboardKeys.Escape:
                    if (Mode == FormModes.Edit)
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
                        await Form.OnSubmit.InvokeAsync(Form.EditContext);
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

                var result = await DialogService.Show<PopupMessage>("", parameters, new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                    NoHeader = true,
                    CloseOnEscapeKey = false,
                }).Result;

                if (!result.Canceled)
                {
                    var url = ItemUrl + "?ignoreGlobalFilters";
                    using (var res = await Http.DeleteAsync(ItemUrl))
                    {
                        await SetValue(await ParseEntityResponse(res));
                    }
                    MadeChanges = true;
                }
            });
        }

        public async Task PrintItem()
        {
            if (Mode >= FormModes.Edit && TaskInProgress != FormTasks.None)
                return;

            await RunTask(FormTasks.Print, async () =>
            {
                //await OnPrint.InvokeAsync();

                //Get a Signed Token to Authenticate /print end-point
                var path = SettingManager.Configuration.ApiPath.AddUrlPath(Action);
                
                var tokenResult = await Http.GetAsync($"{path}/print-token/{Key?.ToString()}");

                var token = await tokenResult.Content.ReadAsStringAsync();

                //Open /print endpoint with the obtained token
                await JsRuntime.InvokeVoidAsync("open", $"{path}/print/{Key?.ToString()}?{token}", "_blank");
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

            HttpResponseMessage res;
            var message = "";

            if (Mode == FormModes.Create)
            {
                var request = Http.CreateIdempotencyRequest(Value, ItemUrl, IdempotencyToken);

                res = await Http.SendAsync(request);

                message = Loc["ItemCreated"];
            }
            else
            {
                res = await Http.PutAsJsonAsync(ItemUrl, Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                message = Loc["ItemSaved"];
            }

            var value = await ParseEntityResponse(res);

            if (value == null)
            {
                return;
            }

            MadeChanges = true;

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

                if (Mode == FormModes.Create)
                {
                    await UpdateUrl(value.ID);
                }
                await SetMode(FormModes.View);
                await SetValue(value);
            }

        }

        internal override async Task SetMode(FormModes mode)
        {
            if (mode == FormModes.Create)
            {
                IdempotencyToken = Guid.NewGuid();
            }
            await base.SetMode(mode);
            SetTitle();
        }

        internal async Task SetValue(T? value, bool copyValue = true)
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
        }

        public void ResizeForm()
        {
            if (MudDialog == null)
            {
                return;
            }

            Maximized = MudDialog.Options.FullScreen != true;
            MudDialog.Options.FullScreen = Maximized;
            MudDialog.SetOptions(MudDialog.Options);
        }

        internal async Task FetchItem(DateTimeOffset? asOf = null)
        {
            await RunTask(FormTasks.Fetch, async () =>
            {
                try
                {
                    var url = asOf == null ? ItemUrl : ItemUrl + "?asOf=" + Uri.EscapeDataString((asOf.Value).ToString("O"));

                    using (var res = await Http.GetAsync(url))
                    {
                        //res.Headers.TryGetValues(Constants.HttpHeaderVersioning, out IEnumerable<string>? versioning);
                        IsTemporal = true; //versioning?.Contains("Temporal") == true;
                        await SetValue(await ParseEntityResponse(res), asOf == null);
                    }
                }
                catch (Exception)
                {
                    EditContext = new EditContext(Value);
                    throw;
                }
                
            });
        }

        internal async Task<T?> ParseEntityResponse(HttpResponseMessage res)
        {
            await OnResponse.InvokeAsync(res);

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
                var parameters = new DialogParameters {
                    { "Message", result.Message },
                    { "Color", Color.Error },
                    { "Icon", Icons.Material.Filled.Error },
                };

                DialogService.Show<PopupMessage>("", parameters, new DialogOptions
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
                await OnEntityResponse.InvokeAsync(value);
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
                DocumentTitle = Loc["ViewingForm", Title, Key];
            }
            else if (Key != null && Mode == FormModes.Edit)
            {
                DocumentTitle = Loc["EditingForm", Title, Key];
            }
            else if (Mode == FormModes.Create)
            {
                DocumentTitle = Loc["CreatingForm", Title];
            }
        }

        internal async Task UpdateUrl(object? key)
        {
            if (key == null || key == default)
                return;

            Key = key;
            await KeyChanged.InvokeAsync(key);

            if (MudDialog == null)
            {
                NavManager.NavigateTo(NavManager.Uri.AddUrlPath(key.ToString()));
            }
            else
            {
                ShiftModal.UpdateKey(key);
            }
        }

        internal async Task OpenInNewTab()
        {
            var url = await JsRuntime.InvokeAsync<string>("GetUrl");

            var modals = ShiftModal.ParseModalUrl(url);
            await ShiftModal.Open(modals.First().Name, Key, ModalOpenMode.NewTab, modals.First().Parameters);
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
                    {"EntitySet", Action.AddUrlPath(Key?.ToString(), "revisions")},
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

                var result = await DialogService.Show<RevisionViewer>("", dParams, options).Result;
                date = (DateTimeOffset?)result.Data;
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
}
