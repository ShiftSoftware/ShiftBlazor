using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Dtos;
using static ShiftSoftware.ShiftBlazor.Utils.Form;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftEntityForm<T> : ShiftFormBasic<T> where T : ShiftEntityDTO, new()
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] private ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;

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
                return Mode == Modes.Create ? path : path.AddUrlPath(Key?.ToString());
            }
        }

        internal override bool HideSubmit
        {
            get => Mode < Modes.Edit ? true : base.HideSubmit;
            set => base.HideSubmit = value;
        }

        internal override string _SubmitText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubmitText)) return Mode == Modes.Create ? "Create" : "Save";
                return base._SubmitText;
            }
            set => base._SubmitText = value;
        }

        protected override async Task OnInitializedAsync()
        {

            if (Key == null && Mode != Modes.Create)
            {
                await SetMode(Modes.Create);
            }

            if (Mode != Modes.Create)
            {
                await FetchItem();
            }

            OriginalValue = JsonSerializer.Serialize(Value);
        }

        internal void FieldChangeHandler(object? sender = null, FieldChangedEventArgs? args = null)
        {
            if (JsonSerializer.Serialize(Value).Equals(OriginalValue))
            {
                editContext.MarkAsUnmodified();
            }
            StateHasChanged();
        }

        public async Task DeleteItem()
        {
            await RunTask(Tasks.Delete, async () =>
            {
                var dialogOptions = new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                };

                var result = await DialogService.ShowMessageBox(
                        "Warning",
                        "Do you want to delete this item?",
                        yesText: "Delete",
                        cancelText: "Cancel",
                        options: dialogOptions);

                if (result.HasValue && result.Value)
                {
                    var url = ItemUrl + "?ignoreGlobalFilters";
                    using var res = await Http.DeleteAsync(ItemUrl);
                    await SetValue(await ParseEntityResponse(res));
                }
            });
        }

        public async Task PrintItem()
        {
            await RunTask(Tasks.Print, async () =>
            {
                await OnPrint.InvokeAsync();
            });
        }

        public async Task EditItem()
        {
            await SetMode(Modes.Edit);
        }

        public async Task CancelChanges()
        {
            if (TaskInProgress != Tasks.None)
            {
                return;
            }

            if (await ConfirmClose())
            {
                await SetMode(Modes.View);
                await RestoreOriginalValue();
            }
        }

        internal override async Task ValidSubmitHandler(EditContext context)
        {
            await RunTask(Tasks.Save, async () =>
            {
                await OnValidSubmit.InvokeAsync(context);

                HttpResponseMessage res;
                var message = "";

                if (Mode == Modes.Create)
                {
                    res = await Http.PostAsJsonAsync(ItemUrl, Value);
                    message = "Item has been Created";
                }
                else
                {
                    res = await Http.PutAsJsonAsync(ItemUrl, Value);
                    message = "Item has been Saved";
                }

                var value = await ParseEntityResponse(res);

                if (value == null)
                {
                    return;
                }

                if (Settings.CloseFormOnSave)
                {
                    MudDialog?.Cancel();
                }
                else if (Settings.ResetFormOnSave)
                {
                    await SetValue(new T());
                }
                else
                {
                    ShowAlert(message, Severity.Success, 5);

                    await UpdateUrl(value.ID);
                    await SetMode(Modes.View);
                    await SetValue(value);
                }
            });

        }

        internal override async Task SetMode(Modes mode)
        {
            await base.SetMode(mode);
            SetTitle();
        }

        internal async Task SetValue(T? value, bool copyValue = true)
        {
            await base.SetValue(value);

            editContext.OnFieldChanged += FieldChangeHandler;

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
            await RunTask(Tasks.Fetch, async () =>
            {
                var url = asOf == null ? ItemUrl : ItemUrl + "?asOf=" + asOf;
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

            var result = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();

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

            if (TaskInProgress == Tasks.Save && result.Message != null)
            {
                await DialogService.ShowMessageBox(result.Message.Title, MessageToHtml(result.Message), options: new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                });

                return null;
            }

            throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
        }

        internal void SetTitle()
        {
            switch (Mode)
            {
                case Modes.View:
                    DocumentTitle = "Viewing";
                    break;
                case Modes.Edit:
                    DocumentTitle = "Editing";
                    break;
                case Modes.Create:
                    DocumentTitle = "Creating new";
                    break;
            }

            DocumentTitle += " " + Title;

            if (Key != null)
            {
                DocumentTitle += " " + Key;
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

            await RunTask(Tasks.FetchRevisions, async () =>
            {
                var path = SettingManager.Configuration.ODataPath.AddUrlPath(Action, Key?.ToString(), "revisions");
                var res = await Http.GetFromJsonAsync<ODataDTO<List<RevisionDTO>>>(path);

                if (res == null)
                {
                    return;
                }

                var dParams = new DialogParameters
                {
                    {"Revisions", res.Value},
                    {"Title", Title + " Revisions" },
                };

                var options = new DialogOptions
                {
                    NoHeader = true,
                };

                var result = await DialogService.Show<RevisionViewer>("", dParams, options).Result;
                date = (DateTime?)result.Data;
            });

            if (date != null)
            {
                await FetchItem(date);
                await SetMode(Modes.Archive);
            }

        }

        internal async Task CloseRevision()
        {
            await SetMode(Modes.View);
            await RestoreOriginalValue();
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

        //helpers
        internal string? MessageToHtml(Message message)
        {
            var body = message?.Body;
            var subMessages = "";

            if (message?.SubMessages != null)
            {
                foreach (var item in message.SubMessages)
                {
                    subMessages += $"<strong>{item.Title}</strong>";
                    if (!string.IsNullOrWhiteSpace(item.Body))
                    {
                        subMessages += $": {item.Body}";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(subMessages))
            {
                body += $"<br/><ul>{subMessages}</ul>";
            }

            return body;
        }
    }
}
