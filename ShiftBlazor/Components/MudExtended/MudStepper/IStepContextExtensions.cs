using System.Runtime.CompilerServices;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Components;

public static class StepContextExtensions
{
    private static readonly ConditionalWeakTable<IStepContext, StrongBox<bool>> s_validationErrors = new();

    extension(IStepContext step)
    {
        public bool HasValidationError
        {
            get => s_validationErrors.TryGetValue(step, out var box) && box.Value;
            set
            {
                if (s_validationErrors.TryGetValue(step, out var box))
                    box.Value = value;
                else
                    s_validationErrors.Add(step, new StrongBox<bool>(value));
            }
        }
    }
}
