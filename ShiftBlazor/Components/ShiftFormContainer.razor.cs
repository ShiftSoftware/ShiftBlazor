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

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftFormContainer<T> : ComponentBase where T : ShiftEntityDTO, new()
    {
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] NavigationManager NavManager { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] MessageService MsgService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance? MudDialog { get; set; }
        [Parameter] public States State { get; set; } = States.View;
        [Parameter] public States StateBeforeSaving { get; set; }
        [Parameter] public EventCallback<States> StateChanged { get; set; }
        [Parameter] public T Value { get; set; } = new T();
        [Parameter] public EventCallback<T> ValueChanged { get; set; }
        [Parameter] public EventCallback<T> OnServerResponse { get; set; }
        [Parameter] public string? Action { get; set; }
        [Parameter] public string? Title { get; set; }
        [Parameter] public FormSettings Settings { get; set; } = new FormSettings();
        [Parameter] public object? Key { get; set; }
        [Parameter] public EventCallback<object?> KeyChanged { get; set; }
        [Parameter] public EventCallback<bool> OnSave { get; set; }
        [Parameter] public EventCallback<T> OnBeforeEdit { get; set; }
        [Parameter] public EventCallback<ShiftEntityResponse<T>> OnEditResponse { get; set; }
        [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
        [Parameter] public RenderFragment? FooterActionsTemplate { get; set; }
        [Parameter] public RenderFragment? HeaderTemplate { get; set; }
        [Parameter] public RenderFragment? FooterTemplate { get; set; }
        [Parameter] public RenderFragment? ViewStateTemplate { get; set; }
        [Parameter] public RenderFragment? InsertStateTemplate { get; set; }
        [Parameter] public bool HidePrint { get; set; }
        [Parameter] public bool HideDelete { get; set; }
        [Parameter] public bool HideEdit { get; set; }
        [Parameter] public bool DisablePrint { get; set; }
        [Parameter] public bool DisableDelete { get; set; }
        [Parameter] public bool DisableEdit { get; set; }
        [Parameter] public EventCallback OnPrint { get; set; }
        [Parameter] public string IconSvg { get; set; } = @Icons.Material.Filled.ListAlt;

        private bool Printing { get; set; }

        private string? OriginalValue { get; set; }
        private string SubmitText { get; set; } = "";
        private string EditText { get; set; } = "Edit";
        private string CancelText { get; set; } = "";
        private bool IsCrud { get; set; } = false;
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

            UpdateButtonTexts(State);

            if (IsCrud && State != States.Create)
            {
                await GetItem();
            }

            OriginalValue = JsonSerializer.Serialize(Value);
        }

        private async Task CancelChanges()
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

        private void UpdateButtonTexts(States mode)
        {
            if (mode == States.Create)
            {
                SubmitText = "Create";
                CancelText = "Cancel";
            }
            else if (mode == States.Edit)
            {
                SubmitText = "Save";
                CancelText = "Cancel";
            }
            else if (mode == States.View)
            {
                SubmitText = "Edit";
                CancelText = "Close";
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
            UpdateButtonTexts(State);
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

        private void Cancel()
        {
            if (State == States.Edit && IsCrud)
            {
                _ = CancelChanges();
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
                using (var res = await Http.DeleteAsync(ItemUrl))
                {
                    if (res.StatusCode == HttpStatusCode.NoContent)
                    {
                        Value = new T();
                    }
                    else if (res.IsSuccessStatusCode)
                    {
                        var value = await res.Content.ReadFromJsonAsync<T>();
                        UpdateValue(value);
                        await this.OnServerResponse.InvokeAsync(value);
                    }
                    else
                    {
                        throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(res.ReasonPhrase));
                    }
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

        private async Task Test(EditContext context)
        {
            MsgService.Info("Can't save");
        }

        private async Task OnValidSubmit(EditContext context)
        {
            MsgService.Info("Saving");
            await OnSave.InvokeAsync(true);
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

                shiftEntityResponse = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>();

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
            if (MudDialog == null && Value.ID == default)
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
                        var value = await res.Content.ReadFromJsonAsync<T>();
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
    }
}
