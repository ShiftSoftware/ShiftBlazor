using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ActionButton<T> : MudButtonExtended, IEntityRequestComponent<IList<T>>
    where T : ShiftEntityDTOBase, new()
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] public HttpClient HttpClient { get; private set; } = default!;
    [Inject] MessageService MessageService { get; set; } = default!;
    [Inject] public SettingManager SettingManager { get; private set; } = default!;
    [Inject] public ShiftBlazorLocalizer Loc { get; private set; } = default!;


    [CascadingParameter]
    public ShiftList<T>? ShiftListGeneric { get; set; }


    /// <summary>
    /// When true, display a dialog to confirm the action.
    /// </summary>
    [Parameter]
    public bool Confirm { get; set; }

    /// <summary>
    /// The type of the component to be opened on click.
    /// </summary>
    [Parameter]
    public Type? ComponentType { get; set; }

    /// <summary>
    /// Executes before the default confirmation dialog is shown.
    /// </summary>
    [Parameter]
    public Func<SelectState<T>, ValueTask<bool>>? OnDefaultDialogOpen { get; set; }


    /// <summary>
    /// A replacement for OnClick that has a SelectState object as an argument.
    /// </summary>
    [Parameter]
    public Func<SelectState<T>, ValueTask<bool>>? OnClickGetItems { get; set; }

    /// <summary>
    /// The URL endpoint path that the SelectState will be sent to on button click.
    /// </summary>
    [Parameter]
    public string? Endpoint { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    /// <summary>
    /// The confirmation dialog title.
    /// </summary>
    [Parameter]
    public string? DialogTitle { get; set; }

    /// <summary>
    /// The confirmation dialog text, uses string template with a Select Items Count parameter,
    /// will be ignored if DialogBodyTemplate has value.
    /// </summary>
    [Parameter]
    public string? DialogTextTemplate { get; set; }

    /// <summary>
    /// The confirmation dialog icon.
    /// </summary>
    [Parameter]
    public string? DialogIcon { get; set; } = Icons.Material.Filled.Warning;

    /// <summary>
    /// The confirmation dialog color.
    /// </summary>
    [Parameter]
    public Color? DialogColor { get; set; } = Color.Error;

    /// <summary>
    /// The confirmation dialog Confirm button text.
    /// </summary>
    [Parameter]
    public string? DialogConfirmText { get; set; }

    /// <summary>
    /// The confirmation dialog Cancel button text.
    /// </summary>
    [Parameter]
    public string? DialogCancelText { get; set; }

    /// <summary>
    /// The confirmation dialog size.
    /// </summary>
    [Parameter]
    public MaxWidth DialogWidth { get; set; } = MaxWidth.ExtraSmall;

    [Parameter]
    public bool NoHeader { get; set; }

    /// <summary>
    /// The confirmation dialog body template, this will replace the DialogTextTemplate.
    /// </summary>
    [Parameter]
    public RenderFragment<SelectState<T>>? DialogBodyTemplate { get; set; }

    /// <summary>
    /// Will display message as a snackbar when task is finished successfully.
    /// </summary>
    [Parameter]
    public string? TaskSuccessMessage { get; set; }

    /// <summary>
    /// Will display message as a snackbar when task has failed.
    /// </summary>
    [Parameter]
    public string? TaskFailMessage { get; set; }

    [Parameter]
    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }
    [Parameter]
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }
    [Parameter]
    public Func<Exception, ValueTask<bool>>? OnError { get; set; }
    [Parameter]
    public Func<ShiftEntityResponse<IList<T>>?, ValueTask<bool>>? OnResult { get; set; }


    internal Guid IdempotencyToken = Guid.NewGuid();
    internal bool HasOnClickDelegate;
    public object? Key { get; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Check if OnClick has a method provided by the user
        // before we inject our own method in OnParametersSet.
        parameters.TryGetValue(nameof(OnClick), out EventCallback<MouseEventArgs> onClick);
        HasOnClickDelegate = onClick.HasDelegate;

        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        // Since this component is intended to be used with DataGrid,
        // disable the button when there are no items selected in the DataGrid.
        Disabled = ShiftListGeneric?.SelectState.Count == 0;

        // Inject our own custom methods to the MudButton's OnClick method
        // to override the default behavior of the action.
        if (HasOnClickDelegate)
        {
            var originalClickMethod = OnClick;
            // delegate needs to be async to catch exception.
            OnClick = CreateEvent(async () =>
            {
                var success = true;
                try
                {
                    await originalClickMethod.InvokeAsync(null);
                }
                catch (Exception)
                {
                    success = false;
                }

                return success;
            });
        }
        else if (ComponentType != null)
        {
            OnClick = CreateEvent(OpenComponentOnClick);
        }
        else if (OnClickGetItems != null)
        {
            OnClick = CreateEvent(RunFuncOnClick);
        }
        else if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            OnClick = CreateEvent(SendRequest);
        }

        base.OnParametersSet();
    }

    private async ValueTask<bool?> SendRequest()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
            {
                throw new ArgumentNullException(nameof(Endpoint), $"{nameof(Endpoint)} cannot be null or empty.");
            }

            var url = IRequestComponent.GetPath(this);

            using var request = HttpClient.CreatePostRequest(ShiftListGeneric?.SelectState ?? new(), url, IdempotencyToken);

            if (OnBeforeRequest != null && await OnBeforeRequest.Invoke(request))
            {
                return null;
            }

            using var response = await HttpClient.SendAsync(request);

            if (OnResponse != null && await OnResponse.Invoke(response))
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ShiftEntityResponse<IList<T>>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            if (OnResult != null && await OnResult.Invoke(result))
            {
                return null;
            }

            if (result?.Message != null)
            {
                var parameters = new DialogParameters {
                    { "Message", result.Message },
                    { "Color", Color.Error },
                    { "Icon", Icons.Material.Filled.Error },
                };

                await DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                    NoHeader = true,
                    CloseOnEscapeKey = false,
                });
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            if (OnError != null && await OnError.Invoke(e))
            {
                return false;
            }
        }

        return false;
    }

    private async ValueTask<bool?> RunFuncOnClick()
    {
        if (OnClickGetItems != null)
        {
            try
            {
                return await OnClickGetItems.Invoke(ShiftListGeneric?.SelectState ?? new());
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }

    private async ValueTask<bool?> OpenComponentOnClick()
    {
        if (ComponentType == null)
            return false;

        var parameters = new DialogParameters
        {
            { "Selected", ShiftListGeneric?.SelectState ?? new() },
        };

        var dialogOptions = new DialogOptions
        {
            MaxWidth = DialogWidth,
            NoHeader = NoHeader,
        };

        var dialogReference = await DialogService.ShowAsync(ComponentType, "", parameters);
        var result = await dialogReference.Result;
        return !result?.Canceled;
    }

    private EventCallback<MouseEventArgs> CreateEvent(Func<ValueTask<bool?>> action)
    {
        var func = async delegate (MouseEventArgs args)
        {
            // Inject loading icon into the button's ChildContent
            var originalChildContent = ChildContent;
            ChildContent = builder =>
            {
                var color = ShiftListGeneric?.NavIconFlatColor == true ? Color.Inherit : Color;
                builder.OpenComponent<MudProgressCircular>(0);
                builder.AddAttribute(1, "Color", color);
                builder.AddAttribute(2, "Indeterminate", true);
                builder.AddAttribute(3, "Size", Size.Small);
                builder.CloseComponent();
                builder.AddContent(4, originalChildContent);
            };
            StateHasChanged();

            try
            {
                bool? success = false;
                var userCanceled = false;
                var selectCount = ShiftListGeneric?.SelectState.Count ?? 0;

                if (Confirm)
                {
                    var defaultText = selectCount == 1 ? Loc["DialogDefaultText"] : Loc["DialogDefaultTextPlural", selectCount];
                    var text = DialogTextTemplate ?? defaultText;
                    var title = DialogTitle ?? Loc["DialogDefaultTitle"];
                    var confirmText = DialogConfirmText ?? Loc["DialogDefaultConfirm"];
                    var cancelText = DialogCancelText ?? Loc["DialogDefaultCancel"];
                    var message = new Message(title, string.Format(text, selectCount));

                    RenderFragment<Message>? messageTemplate = null;

                    if (DialogBodyTemplate != null && ShiftListGeneric != null)
                    {
                        // Use RenderTreeBuilder to change the template type.
                        messageTemplate = (msg) => builder => builder.AddContent(0, DialogBodyTemplate(ShiftListGeneric.SelectState));
                    }

                    var parameters = new DialogParameters
                    {
                        { "Message", message },
                        { "MessageBodyTemplate", messageTemplate },
                        { "Color", DialogColor },
                        { "Icon", DialogIcon },
                        { "ConfirmText",  confirmText},
                        { "CancelText",  cancelText },
                    };

                    if (this.OnDefaultDialogOpen is not null && ShiftListGeneric is not null)
                    {
                        var continueWithShowingTheDialog = await this.OnDefaultDialogOpen.Invoke(ShiftListGeneric.SelectState);

                        if (!continueWithShowingTheDialog)
                        {
                            ChildContent = originalChildContent;

                            StateHasChanged();

                            return;
                        }
                    }

                    var dialogReference = await DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
                    {
                        MaxWidth = DialogWidth,
                        NoHeader = true,
                        CloseOnEscapeKey = false,
                    });

                    var result = await dialogReference.Result;

                    userCanceled = result?.Canceled == true;
                    // Only reload if user doesn't cancel the confirmation dialog.
                    if (!userCanceled)
                    {
                        success = await action.Invoke();
                    }
                }
                else
                {
                    success = await action.Invoke();
                }

                if (success.HasValue)
                {
                    if (success.Value && TaskSuccessMessage != null && !userCanceled)
                    {
                        MessageService.Success(string.Format(TaskSuccessMessage, selectCount));
                    }
                    else if (!success.Value && TaskFailMessage != null && !userCanceled)
                    {
                        MessageService.Error(string.Format(TaskFailMessage, selectCount));
                    }

                    if (success.Value && ShiftListGeneric?.DataGrid != null)
                    {
                        IdempotencyToken = Guid.NewGuid();
                        await ShiftListGeneric.DataGrid.ReloadServerData();
                    }
                }

            }
            catch (Exception e)
            {
                MessageService.Error(Loc["ActionButtonCreateError"], e.Message, e.ToString());
            }

            // Restore ChildConent to original state when task is finished
            ChildContent = originalChildContent;
            StateHasChanged();
        };

        return new EventCallback<MouseEventArgs>(this, func);
    }
}
