using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocompleteWithQuickAdd<T, TEntitySet, TQuickAdd> : ShiftAutocomplete<T, TEntitySet>
    {
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;

        [Parameter, EditorRequired]
        public Type? QuickAddComponentType { get; set; }
        [Parameter]
        public string? QuickAddParameterName { get; set; }

        internal bool AdornmentIconIsNotSet = false;
        internal bool AdornmentAriaLabelIsNotSet = false;
        internal bool OnAdornmentClickIsNotSet = false;

        [Parameter]
        public Func<TQuickAdd, T>? Convert { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            Type? type;
            parameters.TryGetValue(nameof(QuickAddComponentType), out type);

            if (type == null)
            {
                return base.SetParametersAsync(parameters);
            }

            string? adornmentIcon;
            string? adornmentAriaLabel;
            EventCallback<MouseEventArgs>? onAdornmentClick;

            parameters.TryGetValue(nameof(AdornmentIcon), out adornmentIcon);
            parameters.TryGetValue(nameof(AdornmentAriaLabel), out adornmentAriaLabel);
            parameters.TryGetValue(nameof(OnAdornmentClick), out onAdornmentClick);

            AdornmentIconIsNotSet = adornmentIcon == null;
            AdornmentAriaLabelIsNotSet = adornmentAriaLabel == null;
            OnAdornmentClickIsNotSet = onAdornmentClick == null;

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

            var value = odataResultType.GetProperty(DataValueField)?.GetValue(result.Data)?.ToString();
            var text = odataResultType.GetProperty(DataTextField)?.GetValue(result.Data)?.ToString();

            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }
                
            Value = new T
            {
                Value = value,
                Text = text,
            };

            await ValueChanged.InvokeAsync(Value);
        }
    }
}
