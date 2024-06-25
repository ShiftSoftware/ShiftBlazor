using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ActionButton<T> : MudButtonExtended
    where T : ShiftEntityDTOBase, new()
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] MessageService MessageService { get; set; } = default!;
    [Inject] private SettingManager SettingManager { get; set; } = default!;
    [Inject] IStringLocalizer<Resources.Components.ActionButton> Loc { get; set; } = default!;


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
    /// A replacement for OnClick that has a SelectState object as an argument.
    /// </summary>
    [Parameter]
    public Func<SelectState<T>, ValueTask<bool>>? OnClickGetItems { get; set; }

    /// <summary>
    /// The URL endpoint path that the SelectState will be sent to on button click.
    /// </summary>
    [Parameter]
    public string? Action { get; set; }

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

    /// <summary>
    /// The confirmation dialog body template, this will replace the DialogTextTemplate.
    /// </summary>
    [Parameter]
    public RenderFragment<SelectState<T>>? DialogBodyTemplate { get; set; }


    internal Guid IdempotencyToken = Guid.NewGuid();
    internal bool HasOnClickDelegate;

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
            OnClick = CreateEvent(() =>
            {
                originalClickMethod.InvokeAsync(null);
                return new ValueTask<bool>(true);
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
        else if (!string.IsNullOrWhiteSpace(Action))
        {
            OnClick = CreateEvent(SendRequest);
        }
    }

    private async ValueTask<bool> SendRequest()
    {
        if (!string.IsNullOrWhiteSpace(Action))
        {
            string? baseUrl = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
            baseUrl = baseUrl?.AddUrlPath(Action) ?? SettingManager.Configuration.ApiPath.AddUrlPath(Action);

            var request = Http.CreateIdempotencyRequest(ShiftListGeneric?.SelectState ?? new(), baseUrl, IdempotencyToken);
            var response = await Http.SendAsync(request);

            var result = await response.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });

            if (result?.Message != null)
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
            }

            return response.IsSuccessStatusCode;
        }

        return false;
    }

    private async ValueTask<bool> RunFuncOnClick()
    {
        if (OnClickGetItems != null)
        {
            return await OnClickGetItems.Invoke(ShiftListGeneric?.SelectState ?? new());
        }

        return false;
    }

    private async ValueTask<bool> OpenComponentOnClick()
    {
        var parameters = new DialogParameters
        {
            { "Selected", ShiftListGeneric?.SelectState ?? new() },
        };

        var result = await (DialogService.Show(ComponentType, "", parameters).Result);
        return !result.Canceled;
    }

    private EventCallback<MouseEventArgs> CreateEvent(Func<ValueTask<bool>> action)
    {
        var func = async delegate (MouseEventArgs args)
        {
            // Inject loading icon into the button's ChildContent
            var originalChildContent = ChildContent;
            ChildContent = builder =>
            {
                var color = ShiftListGeneric?.NavIconFlatColor == true ? Color.Inherit : Color.Default;
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
                var reload = false;

                if (Confirm)
                {
                    var selectCount = ShiftListGeneric?.SelectState.Count ?? 0;
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

                    var result = await DialogService.Show<PopupMessage>("", parameters, new DialogOptions
                    {
                        MaxWidth = DialogWidth,
                        NoHeader = true,
                        CloseOnEscapeKey = false,
                    }).Result;

                    // Only reload if user doesn't cancel the confirmation dialog.
                    if (!result.Canceled)
                    {
                        reload = await action.Invoke();
                    }
                }
                else
                {
                    reload = await action.Invoke();
                }

                if (reload && ShiftListGeneric?.DataGrid != null)
                {
                    IdempotencyToken = Guid.NewGuid();
                    await ShiftListGeneric.DataGrid.ReloadServerData();
                }

            }
            catch (Exception e)
            {
                MessageService.Error($"Could not execute this action.", e.Message, e.ToString());
            }

            // Restore ChildConent to original state when task is finished
            ChildContent = originalChildContent;
            StateHasChanged();
        };

        return new EventCallback<MouseEventArgs>(this, func);
    }
}
