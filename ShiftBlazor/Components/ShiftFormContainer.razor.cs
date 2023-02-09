using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using static ShiftSoftware.ShiftBlazor.Utils.Form;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Dtos;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using FluentValidation;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftFormContainer<T> : ComponentBase where T : ShiftEntityDTO, new()
    {
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] NavigationManager NavManager { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] MessageService MsgService { get; set; } = default!;

        [CascadingParameter]
        protected MudDialogInstance? MudDialog { get; set; }

        /// <summary>
        /// The current State of the form.
        /// </summary>
        [Parameter]
        public States State { get; set; } = States.View;

        [Parameter]
        public States StateBeforeSaving { get; set; }

        /// <summary>
        /// An event triggered when the state of the State paramater has changed.
        /// </summary>
        [Parameter]
        public EventCallback<States> StateChanged { get; set; }

        /// <summary>
        /// The current item being view/edited, this will be fetched from the API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public T Value { get; set; } = new T();

        /// <summary>
        /// An event triggered when the state of Value has changed.
        /// </summary>
        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        /// <summary>
        /// An event triggered after getting a response from API.
        /// </summary>
        [Parameter]
        public EventCallback<T> OnServerResponse { get; set; }

        /// <summary>
        /// The URL endpoint that processes the CRUD operations.
        /// </summary>
        [Parameter]
        public string? Action { get; set; }

        /// <summary>
        /// The title to render on the form header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// The form settings
        /// </summary>
        [Parameter]
        public FormSettings Settings { get; set; } = new FormSettings();

        /// <summary>
        /// The item ID, it is also used for the CRUD operations.
        /// </summary>
        [Parameter]
        public object? Key { get; set; }

        /// <summary>
        /// An event triggered when the state of Key has changed.
        /// </summary>
        [Parameter]
        public EventCallback<object?> KeyChanged { get; set; }

        /// <summary>
        /// An event triggered before user submits a valid form.
        /// </summary>
        [Parameter]
        public EventCallback<T> OnBeforeEdit { get; set; }

        /// <summary>
        /// An event triggered after saving/editing an item and then getting a response.
        /// </summary>
        [Parameter]
        public EventCallback<ShiftEntityResponse<T>> OnEditResponse { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Used to add custom elements to the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarTemplate { get; set; }

        /// <summary>
        /// Used to override the footer toolbar buttons.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterActionsTemplate { get; set; }

        /// <summary>
        /// Used to override the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        /// Used to override the footer.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterTemplate { get; set; }

        /// <summary>
        /// Template element to render elements only when form is in View State.
        /// </summary>
        [Parameter]
        public RenderFragment? ViewStateTemplate { get; set; }

        /// <summary>
        /// Template element to render elements when not in View State.
        /// </summary>
        [Parameter]
        public RenderFragment? InsertStateTemplate { get; set; }

        /// <summary>
        /// Specifies whether to hide Print button or not.
        /// </summary>
        [Parameter]
        public bool HidePrint { get; set; }

        /// <summary>
        /// Specifies whether to hide Delete button or not.
        /// </summary>
        [Parameter]
        public bool HideDelete { get; set; }

        /// <summary>
        /// Specifies whether to hide Edit button or not.
        /// </summary>
        [Parameter]
        public bool HideEdit { get; set; }

        /// <summary>
        /// Specifies whether to disable Print button or not.
        /// </summary>
        [Parameter]
        public bool DisablePrint { get; set; }

        /// <summary>
        /// Specifies whether to disable Delete button or not.
        /// </summary>
        [Parameter]
        public bool DisableDelete { get; set; }

        /// <summary>
        /// Specifies whether to disable Edit button or not.
        /// </summary>
        [Parameter]
        public bool DisableEdit { get; set; }

        /// <summary>
        /// An event triggered when Print button is clicked, by default Print button does nothing.
        /// </summary>
        [Parameter]
        public EventCallback OnPrint { get; set; }

        /// <summary>
        /// The icon displayed before the Form Title, in a string in SVG format.
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = @Icons.Material.Filled.ListAlt;

        /// <summary>
        /// Model Validator object.
        /// If Validator is not set, reflection will be used to find the model validator.
        /// Otherwise if it is set, reflection will be disabled and DataAnnotationsValidator will also be disabled.
        /// </summary>
        [Parameter]
        public AbstractValidator<T>? Validator { get; set; }

        private bool Printing { get; set; }
        private string? OriginalValue { get; set; }
        private bool IsCrud { get; set; } = false;
        private string DocumentTitle = "";
        private string ItemUrl { 
            get {
                return Action + Key;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (Key == null)
            {
                State = States.Create;
            }

            IsCrud = Action != null;

            if (!IsCrud)
            {
                Settings.CloseFormOnSave = !Settings.ResetFormOnSave;
            }

            _ = StateChanged.InvokeAsync(State);

            if (IsCrud && State != States.Create)
            {
                await GetItem();
            }

            //_fluentValidationValidator.Validator = Validator;

            OriginalValue = JsonSerializer.Serialize(Value);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            SetTitle();
        }

        private void CancelChanges()
        {
            UpdateState(States.View);
            if (string.IsNullOrWhiteSpace(OriginalValue))
            {
                UpdateValue(new T());
            }
            else
            {
                UpdateValue(JsonSerializer.Deserialize<T>(OriginalValue), false);
            }
        }

        private async void UpdateState(States? mode = null)
        {
            if (mode == null)
            {
                State = State == States.View ? States.Edit : States.View;
            }
            else
            {
                State = mode.Value;
            }
            await StateChanged.InvokeAsync(State);
        }

        private async void UpdateValue(T? value, bool updateOriginalValue = true)
        {
            if (value != null)
            {
                Value = value;
                await ValueChanged.InvokeAsync(value);

                if (updateOriginalValue)
                {
                    OriginalValue = JsonSerializer.Serialize(value);
                }
            }
        }

        private void OnEditButton()
        {
            UpdateState(States.Edit);
        }

        private async Task Cancel(bool closeModal = false)
        {
            if (JsonSerializer.Serialize(Value) != OriginalValue)
            {
                var dialogOptions = new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                };

                bool? result = await DialogService.ShowMessageBox(
                    "Warning",
                    "You have unsaved changes, do you want to cancel?",
                    yesText: "Yes",
                    cancelText: "No",
                    options: dialogOptions);

                if (!result.HasValue || result.HasValue && !result.Value)
                {
                    return;
                }
            }

            if (State == States.Edit && IsCrud && !closeModal)
            {
                CancelChanges();
            }
            else
            {
                CloseDialog();
            }
        }

        private async Task Delete()
        {
            if (!IsCrud)
            {
                return;
            }
            var dialogOptions = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
            };

            bool? result = await DialogService.ShowMessageBox(
                "Warning",
                "Do you want to delete this item?",
                yesText: "Delete",
                cancelText: "Cancel",
                options: dialogOptions);

            if (result.HasValue && result.Value)
            {
                using var res = await Http.DeleteAsync(ItemUrl);

                if (res.StatusCode == HttpStatusCode.NoContent)
                {
                    Value = new T();
                }
                else if (res.IsSuccessStatusCode)
                {
                    var shiftResponse = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();
                    var value = shiftResponse?.Entity;
                    if (value != null)
                    {
                        UpdateValue(value);
                        await this.OnServerResponse.InvokeAsync(value);
                    }
                }
                else
                {
                    throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
                }
            }
        }

        private async Task Print()
        {
            this.Printing = true;

            await this.OnPrint.InvokeAsync();

            this.Printing = false;
        }

        private void CloseDialog()
        {
            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
            }
        }

        private void OnInvalidSubmit(EditContext context)
        {
            // display silent errors/validations
        }

        private async Task OnValidSubmit(EditContext context)
        {
            await OnBeforeEdit.InvokeAsync(Value);

            if (!IsCrud)
            {
                return;
            }

            HttpResponseMessage res;
            ShiftEntityResponse<T>? shiftEntityResponse = null;

            StateBeforeSaving = State;

            State = States.Saving;

            await StateChanged.InvokeAsync(State);

            try
            {
                if (StateBeforeSaving == States.Create)
                {
                    res = await Http.PostAsJsonAsync(ItemUrl, Value);
                }
                else
                {
                    res = await Http.PutAsJsonAsync(ItemUrl, Value);
                }

                try
                {
                    shiftEntityResponse = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();
                }
                catch {}

                State = StateBeforeSaving;

                await StateChanged.InvokeAsync(State);
            }
            catch (Exception e)
            {
                MsgService.Error("Could not save item", e.Message, e.ToString());

                State = StateBeforeSaving;

                await StateChanged.InvokeAsync(State);

                return;
            }

            if (!res.IsSuccessStatusCode)
            {
                if (shiftEntityResponse != null)
                {
                    var msg = shiftEntityResponse.Message;

                    MsgService.Error(msg?.Title ?? "Failed to submit data", msg?.Title, msg?.Body);
                }
                else
                {
                    MsgService.Error("Could not save item, Error: " + res.StatusCode);
                }

                return;
            }

            if (Settings.CloseFormOnSave)
            {
                MudDialog?.Cancel();
            }
            else if (Settings.ResetFormOnSave)
            {
                UpdateValue(new T());

                await this.OnServerResponse.InvokeAsync(new T());
            }
            else
            {
                UpdateState(States.View);
                T? Item = default;

                try
                {
                    if (shiftEntityResponse != null)
                    {
                        Item = shiftEntityResponse.Entity;
                        if (Item != null)
                        {
                            await KeyChanged.InvokeAsync(Item.ID);

                            if (Item.ID != default)
                                UpdateUrl(Item.ID);
                        }
                        await OnEditResponse.InvokeAsync(shiftEntityResponse);
                    }

                    UpdateValue(Item);

                    await this.OnServerResponse.InvokeAsync(Item);
                }
                catch (Exception)
                {
                    MsgService.Error("Something went wrong while trying to retreive the data from the server.");
                    return;
                }
            }

        }

        private void UpdateUrl(object key)
        {
            if (Value.ID != default)
                return;

            if (MudDialog == null)
            {
                NavManager.NavigateTo(NavManager.Uri + "/" + key.ToString());
            }
            else
            {
                ShiftModal.UpdateKey(key);
            }
        }

        private async Task GetItem()
        {
            try
            {
                using (var res = await Http.GetAsync(ItemUrl))
                {
                    if (res.StatusCode == HttpStatusCode.NoContent)
                    {
                        Value = new T();
                    }
                    else if (res.IsSuccessStatusCode)
                    {
                        var shiftResponse = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();
                        var value = shiftResponse?.Entity;
                        UpdateValue(value);
                        await this.OnServerResponse.InvokeAsync(value);
                    }
                    else
                    {
                        throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
                    }
                }
            }
            catch (Exception e)
            {
                MsgService.Error("Could not load item.", e.Message, e.ToString());
            }
        }

        private void Maximize()
        {
            MudDialog.Options.FullScreen = MudDialog.Options.FullScreen != true;
            MudDialog.SetOptions(MudDialog.Options);
        }

        private void SetTitle()
        {
            switch (State)
            {
                case States.View:
                    DocumentTitle = "Viewing";
                    break;
                case States.Edit:
                    DocumentTitle = "Editing";
                    break;
                case States.Create:
                    DocumentTitle = "Creating new";
                    break;
                case States.Saving:
                    DocumentTitle = "Saving";
                    break;
            }

            DocumentTitle += " " + Title;

            if (Key != null)
            {
                DocumentTitle += " " + Key;
            }
        }
    }
}
