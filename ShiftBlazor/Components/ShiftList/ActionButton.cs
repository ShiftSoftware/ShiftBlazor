using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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


    [CascadingParameter]
    public ShiftList<T>? ShiftListGeneric { get; set; }

    [Parameter]
    public bool Confirm { get; set; }

    [Parameter]
    public Type? ComponentType { get; set; }

    [Parameter]
    public Func<SelectState<T>, ValueTask<bool>>? OnClickGetItems { get; set; }

    [Parameter]
    public string? Action { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? DialogTitle { get; set; }

    [Parameter]
    public string? DialogTextTemplate { get; set; }

    [Parameter]
    public string? DialogIcon { get; set; } = Icons.Material.Filled.Warning;

    [Parameter]
    public Color? DialogColor { get; set; } = Color.Error;

    [Parameter]
    public string? DialogConfirmText { get; set; }

    [Parameter]
    public string? DialogCancelText { get; set; }

    internal Guid IdempotencyToken = Guid.NewGuid();
    internal bool HasOnClickDelegate;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue(nameof(OnClick), out EventCallback<MouseEventArgs> onClick);
        HasOnClickDelegate = onClick.HasDelegate;

        return base.SetParametersAsync(parameters);
    }
    protected override void OnParametersSet()
    {
        Disabled = ShiftListGeneric?.SelectState.Count == 0;

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
            try
            {
                if (Confirm)
                {
                    var selectCount = ShiftListGeneric?.SelectState.Count ?? 0;
                    var text = DialogTextTemplate ?? $"Are you sure you want to perform this action on {{0}} item{(selectCount == 1 ? "" : "s")}?";
                    var title = DialogTitle ?? "Continue?";
                    var confirmText = DialogConfirmText ?? "Yes";
                    var cancelText = DialogCancelText ?? "No";

                    var message = new Message
                    {
                        Title = title,
                        Body = string.Format(text, selectCount),
                    };

                    var parameters = new DialogParameters
                    {
                        { "Message", message },
                        { "Color", DialogColor },
                        { "Icon", DialogIcon },
                        { "ConfirmText",  confirmText},
                        { "CancelText",  cancelText },
                    };

                    var result = await DialogService.Show<PopupMessage>("", parameters, new DialogOptions
                    {
                        MaxWidth = MaxWidth.ExtraSmall,
                        NoHeader = true,
                        CloseOnEscapeKey = false,
                    }).Result;

                    if (!result.Canceled)
                    {
                        if (await action.Invoke() && ShiftListGeneric?.DataGrid != null)
                        {
                            IdempotencyToken = Guid.NewGuid();
                            await ShiftListGeneric.DataGrid.ReloadServerData();
                        }
                    }
                }
                else if (await action.Invoke() && ShiftListGeneric?.DataGrid != null)
                {
                    IdempotencyToken = Guid.NewGuid();
                    await ShiftListGeneric.DataGrid.ReloadServerData();
                }

            }
            catch (Exception e)
            {
                MessageService.Error($"Could not execute this action.", e.Message, e.ToString());
            }
        };

        return new EventCallback<MouseEventArgs>(this, func);
    }
}
