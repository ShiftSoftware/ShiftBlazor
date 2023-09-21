using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftEntityForm<T> : ShiftFormBasic<T> where T : ShiftEntityDTO, new()
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [Inject] IStringLocalizer<Resources.Components.ShiftEntityForm> Loc { get; set; } = default!;
        
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
        public bool HidePrint { get; set; }

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

        /// <summary>
        ///     An event triggered when Print button is clicked, by default Print button does nothing.
        /// </summary>
        [Parameter]
        public EventCallback OnPrint { get; set; }

        /// <summary>
        ///     An event triggered after getting a response from API.
        /// </summary>
        [Parameter]
        public EventCallback<HttpResponseMessage> OnResponse { get; set; }

        [Parameter]
        public EventCallback<T> OnEntityResponse { get; set; }

        internal string? OriginalValue { get; set; }
        internal bool Maximized { get; set; }

        internal string ItemUrl
        {
            get
            {
                var path = SettingManager.Configuration.ApiPath.AddUrlPath(Action);
                return Mode == FormModes.Create ? path : path.AddUrlPath(Key?.ToString());
            }
        }

        internal override bool HideSubmit
        {
            get => Mode < FormModes.Edit ? true : base.HideSubmit;
            set => base.HideSubmit = value;
        }

        internal override string _SubmitText
        {
            get => string.IsNullOrWhiteSpace(SubmitText)
                ? Mode == FormModes.Create ? Loc["CreateForm"] : Loc["SaveForm"]
                : base._SubmitText;
            set => base._SubmitText = value;
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

            if (Mode != FormModes.Create)
            {
                if (TypeAuthAction is null || TypeAuthService.Can(TypeAuthAction, TypeAuth.Core.Access.Read))
                    await FetchItem();
            }

            SetTitle();

            OriginalValue = JsonSerializer.Serialize(Value);
        }

        public async Task DeleteItem()
        {
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
            await RunTask(FormTasks.Print, async () =>
            {
                await OnPrint.InvokeAsync();
            });
        }

        public async Task EditItem()
        {
            await SetMode(FormModes.Edit);
        }

        public async Task CancelChanges()
        {
            if (TaskInProgress != FormTasks.None)
            {
                return;
            }

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
                res = await Http.PostAsJsonAsync(ItemUrl, Value);
                message = Loc["ItemCreated"];
            }
            else
            {
                res = await Http.PutAsJsonAsync(ItemUrl, Value);
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
            }
            else if (OnSaveAction == FormOnSaveAction.ResetFormOnSave)
            {
                await SetValue(new T());
            }
            else if (OnSaveAction == FormOnSaveAction.ViewFormOnSave)
            {
                ShowAlert(message, Severity.Success, 5);

                await UpdateUrl(value.ID);
                await SetMode(FormModes.View);
                await SetValue(value);
            }

        }

        internal override async Task SetMode(FormModes mode)
        {
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

        internal async Task FetchItem(DateTime? asOf = null)
        {
            await RunTask(FormTasks.Fetch, async () =>
            {
                var url = asOf == null ? ItemUrl : ItemUrl + "?asOf=" +  (DateTimeOffset) asOf.Value;
                using (var res = await Http.GetAsync(url))
                {
                    await SetValue(await ParseEntityResponse(res), asOf == null);
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
                result = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();
            }
            catch (Exception)
            {
                throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
            }

            if (result == null)
            {
                throw new Exception("Could not get response from server");
            }

            if (res.IsSuccessStatusCode)
            {
                var value = result.Entity;
                await OnEntityResponse.InvokeAsync(value);
                return value;
            }

            if (TaskInProgress == FormTasks.Save && result.Message != null)
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
                });

                return null;
            }

            throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
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
            DateTime? date = null;

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
                };

                var result = await DialogService.Show<RevisionViewer>("", dParams, options).Result;
                date = (DateTime?)result.Data;
            });

            if (date != null)
            {
                if (date.Value.Year > 9000)
                {
                    await CloseRevision();
                }
                else
                {
                    await FetchItem(date);
                    await SetMode(FormModes.Archive);
                }
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

    }
}
