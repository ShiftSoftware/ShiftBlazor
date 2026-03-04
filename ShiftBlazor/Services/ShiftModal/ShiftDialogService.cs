using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Services;

internal class ShiftDialogService : DialogService
{
    private IDialogReference? _dialogReference;

    public void InjectDialog(IDialogReference dialogReference)
    {
        _dialogReference = dialogReference;
    }

    public override IDialogReference CreateReference()
    {
        if (_dialogReference == null)
        {
            return new DialogReference(Guid.NewGuid(), this);
        }

        return _dialogReference;
    }
}
