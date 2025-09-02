using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Collections.ObjectModel;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class DropdownTree
{
    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Text;

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    [Parameter]
    public string? StartIcon { get; set; }

    [Parameter]
    public string? EndIcon { get; set; }

    [Parameter]
    public string? IconClass { get; set; }

    [Parameter]
    public Color IconColor { get; set; } = Color.Inherit;

    [Parameter]
    public Size? IconSize { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    [Parameter]
    public bool Ripple { get; set; } = true;

    [Parameter]
    public bool DropShadow { get; set; } = true;

    [Parameter]
    public DropdownWidth DropdownWidth { get; set; } = DropdownWidth.Relative;

    [Parameter]
    public bool Dense { get; set; }

    [Parameter]
    public string? ListClass { get; set; }

    [Parameter]
    public int? MaxHeight { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? PopoverClass { get; set; }

    [Parameter]
    public Origin AnchorOrigin { get; set; } = Origin.TopLeft;

    [Parameter]
    public Origin TransformOrigin { get; set; } = Origin.TopLeft;

    [Parameter]
    public bool LockScroll { get; set; }

    [Parameter]
    public bool MouseOver { get; set; }

    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object?> UserAttributes { get; set; } = [];

    [Parameter]
    public List<ShiftEntitySelectDTO> Value { get; set; } = [];

    [Parameter]
    public SelectionMode SelectionMode { get; set; } = SelectionMode.SingleSelection;

    [Parameter]
    public EventCallback<string> SelectedValueChanged { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> SelectedValuesChanged { get; set; }

    private bool _isPointerOver;
    private bool _isTemporary;
    public bool IsOpen = false;

    public Task CloseMenuAsync()
    {
        IsOpen = false;
        _isPointerOver = false;
        StateHasChanged();

        return IsOpenChanged.InvokeAsync(IsOpen);
    }

    private RenderFragment CreateTreeList(List<ShiftEntitySelectDTO> dtos)
    {
        return builder =>
        {
            var counter = 0;

            //append each item next to each other
            foreach (var dto in dtos)
            {
                builder.AddContent(counter++, CreateTreeItem(dto));
            }
        };
    }

    private RenderFragment CreateTreeItem(ShiftEntitySelectDTO dto)
    {
        return builder =>
        {
            // create the item component
            builder.OpenComponent<MudTreeViewItem<string>>(0);
            builder.AddAttribute(1, "Text", dto.Text);
            builder.AddAttribute(2, "Value", dto.Value);
            // create nested items
            if (dto.Nested != null)
            {
                builder.AddAttribute(3, "ChildContent", CreateTreeList(dto.Nested));
            }
            builder.CloseComponent();
        };
    }

    public Task ToggleMenuAsync(EventArgs args)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return IsOpen ? CloseMenuAsync() : OpenMenuAsync(args);
    }

    public Task OpenMenuAsync(EventArgs args, bool temporary = false)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        _isTemporary = temporary;

        // Don't open if already open, but let the stuff above get updated.
        if (IsOpen)
        {
            return Task.CompletedTask;
        }

        IsOpen = true;
        StateHasChanged();

        return IsOpenChanged.InvokeAsync(IsOpen);
    }

    private async Task PointerEnterAsync(PointerEventArgs args)
    {
        // The Enter event will be interfere with the Click event on devices that can't hover.
        if (args.PointerType is "touch" or "pen")
        {
            return;
        }

        _isPointerOver = true;

        if (IsOpen || !MouseOver)
        {
            return;
        }

        await OpenMenuAsync(args, true);
    }

    private async Task PointerLeaveAsync()
    {
        // There's no reason to handle the leave event if the pointer never entered the menu.
        if (!_isPointerOver)
        {
            return;
        }

        _isPointerOver = false;

        if (_isTemporary && MouseOver)
        {
            // Wait a bit to allow the cursor to move from the activator to the items popover.
            await Task.Delay(100);

            // Close the menu if, since the delay, the pointer hasn't re-entered the menu or the overlay was made persistent (because the activator was clicked).
            if (!_isPointerOver && _isTemporary)
            {
                await CloseMenuAsync();
            }
        }
    }
}
