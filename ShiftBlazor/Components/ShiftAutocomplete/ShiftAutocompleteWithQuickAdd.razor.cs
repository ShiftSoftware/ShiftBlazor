using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Localization;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocompleteWithQuickAdd<TEntitySet, TQuickAdd> : ShiftAutocomplete<TEntitySet>
        where TEntitySet : ShiftEntityDTOBase
    {
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;
        [Inject] ShiftBlazorLocalizer Loc { get; set; } = default!;

        [Parameter, EditorRequired]
        public Type? QuickAddComponentType { get; set; }
        [Parameter]
        public string? QuickAddParameterName { get; set; }

        internal bool AdornmentIconIsNotSet;
        internal bool AdornmentAriaLabelIsNotSet;
        internal bool OnAdornmentClickIsNotSet;

        [Parameter]
        public Func<TQuickAdd, ShiftEntitySelectDTO>? Convert { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.TryGetValue(nameof(QuickAddComponentType), out Type? type);

            if (type != null)
            {
                parameters.TryGetValue(nameof(AdornmentIcon), out string? adornmentIcon);
                parameters.TryGetValue(nameof(AdornmentAriaLabel), out string? adornmentAriaLabel);
                parameters.TryGetValue(nameof(OnAdornmentClick), out EventCallback<MouseEventArgs>? onAdornmentClick);

                AdornmentIconIsNotSet = adornmentIcon == null;
                AdornmentAriaLabelIsNotSet = adornmentAriaLabel == null;
                OnAdornmentClickIsNotSet = onAdornmentClick == null;
            }

            return base.SetParametersAsync(parameters);
        }

        internal async Task AddEditItem(object? key = null)
        {
            if (QuickAddComponentType == null)
            {
                return;
            }

            Dictionary<string, string>? parameters = null;

            if (QuickAddParameterName != null)
            {
                parameters = new Dictionary<string, string>
                {
                    {QuickAddParameterName, LastTypedValue }
                };
            }

            var result = await ShiftModal.Open(QuickAddComponentType, key, ModalOpenMode.Popup, parameters);

            if (result == null || result.Canceled == true)
            {
                return;
            }

            var odataResultType = typeof(TQuickAdd);

            var value = odataResultType.GetProperty(_DataValueField)?.GetValue(result.Data)?.ToString();
            var text = odataResultType.GetProperty(_DataTextField)?.GetValue(result.Data)?.ToString();

            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }
                
            Value = new ShiftEntitySelectDTO
            {
                Value = value,
                Text = text,
            };

            await ValueChanged.InvokeAsync(Value);
        }

        private void OpenDialogFromDropDown(string? id)
        {
            if (QuickAddComponentType == null || id == null) { return; }

            _ = ShiftModal.Open(QuickAddComponentType, id, Enums.ModalOpenMode.Popup);
             CloseMenuAsync();
        }
    }
}
