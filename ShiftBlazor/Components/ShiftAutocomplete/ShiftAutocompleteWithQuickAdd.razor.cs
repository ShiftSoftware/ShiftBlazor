using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocompleteWithQuickAdd<T, TQuickAdd> : ShiftAutocomplete<T>
    {
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;

        [Parameter]
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
            if (result != null && result.Canceled != true)
            {
                if (Convert == null)
                {
                    Value = (T)result.Data;
                }
                else
                {
                    Value = Convert((TQuickAdd)result.Data);
                }
                await ValueChanged.InvokeAsync(Value);
            }
        }
    }
}
