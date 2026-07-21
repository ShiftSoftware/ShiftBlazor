using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Globalization;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class ImageViewer : IShortcutComponent
{
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] internal IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Files to display. Use this from any component. Each <see cref="ShiftFileDTO"/> that resolves
    /// to an image (by <c>ContentType</c> or file name) is shown as an image; the rest fall back to a
    /// download view.
    /// </summary>
    [Parameter]
    public List<ShiftFileDTO>? Values { get; set; }

    /// <summary>
    /// Files to display as <see cref="UploaderItem"/>s. Used by <see cref="FileUploader"/>. When both
    /// <see cref="Files"/> and <see cref="Values"/> are set, <see cref="Files"/> takes precedence.
    /// </summary>
    [Parameter]
    public List<UploaderItem>? Files { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();

    private const double MinScale = 1;
    private const double MaxScale = 6;
    private const double ScaleStep = 0.25;

    private ElementReference ViewerRef;

    private int currentIndex;
    private double scale = 1;
    private double posX;
    private double posY;
    private int rotation;

    private bool isDragging;
    private double dragStartX;
    private double dragStartY;
    private double dragOriginX;
    private double dragOriginY;

    private bool scrollThumbPending;

    // Normalized, source-agnostic view of the files being displayed (built from Files or Values).
    private readonly List<ViewerItem> items = new();

    private IReadOnlyList<ViewerItem> Items => items;
    private bool HasItems => items.Count > 0;

    private ViewerItem? Current =>
        HasItems && currentIndex >= 0 && currentIndex < items.Count ? items[currentIndex] : null;

    private string? CurrentUrl => Current?.Url;

    // A file with no URL (e.g. a local file not yet uploaded) has nothing to render, so it falls back
    // to the file view.
    private bool CurrentIsImage => Current?.IsImage == true && !string.IsNullOrWhiteSpace(CurrentUrl);

    private bool IsZoomed => scale > MinScale + 0.001;
    private bool IsTransformed => IsZoomed || rotation != 0 || posX != 0 || posY != 0;

    private string ImageTransform => string.Format(
        CultureInfo.InvariantCulture,
        "transform: translate({0}px, {1}px) scale({2}) rotate({3}deg);",
        posX, posY, scale, rotation);

    private string ZoomLabel => Math.Round(scale * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    protected override void OnInitialized()
    {
        if (MudDialog != null)
        {
            IShortcutComponent.Register(this);
        }
    }

    protected override void OnParametersSet()
    {
        BuildItems();

        // Keep the index valid if the caller changes the file list.
        if (currentIndex >= items.Count)
        {
            currentIndex = Math.Max(0, items.Count - 1);
        }
    }

    // Rebuilt on every parameter change so the viewer reflects live edits to the source list. Keys are
    // stable (UploaderItem.Id / the ShiftFileDTO instance), so @key-tracked thumbnails aren't recreated.
    private void BuildItems()
    {
        items.Clear();

        if (Files is { Count: > 0 })
        {
            foreach (var f in Files)
            {
                items.Add(new ViewerItem(f.Id, f.GetFileName(), f.File?.Url, f.IsImage()));
            }
        }
        else if (Values is { Count: > 0 })
        {
            foreach (var f in Values)
            {
                items.Add(new ViewerItem(f, f.Name, f.Url, IsImageFile(f)));
            }
        }
    }

    private static bool IsImageFile(ShiftFileDTO file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        return !string.IsNullOrWhiteSpace(file.Name)
            && MimeTypes.GetMimeType(file.Name).StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try { await ViewerRef.FocusAsync(); } catch { /* element may be gone */ }
        }

        if (scrollThumbPending)
        {
            scrollThumbPending = false;
            try { await JS.InvokeVoidAsync("shiftScrollIntoView", ViewerRef, currentIndex); } catch { /* no-op */ }
        }
    }

    private void Select(int index)
    {
        if (!HasItems) return;

        var count = Items.Count;
        currentIndex = ((index % count) + count) % count; // wrap-around
        ResetTransform();
        scrollThumbPending = true;
    }

    private void Next() => Select(currentIndex + 1);

    private void Prev() => Select(currentIndex - 1);

    private void ResetTransform()
    {
        scale = 1;
        posX = 0;
        posY = 0;
        rotation = 0;
        isDragging = false;
    }

    private void SetScale(double value)
    {
        scale = Math.Clamp(value, MinScale, MaxScale);
        if (!IsZoomed)
        {
            posX = 0;
            posY = 0;
        }
    }

    private void ZoomIn() => SetScale(scale + ScaleStep);

    private void ZoomOut() => SetScale(scale - ScaleStep);

    private void RotateCw() => rotation = (rotation + 90) % 360;

    private void OnWheel(WheelEventArgs e)
    {
        if (!CurrentIsImage) return;
        SetScale(scale + (e.DeltaY < 0 ? ScaleStep : -ScaleStep));
    }

    private void ToggleZoom()
    {
        if (!CurrentIsImage) return;
        if (IsZoomed) SetScale(MinScale);
        else SetScale(2);
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        if (!CurrentIsImage || !IsZoomed || e.Button != 0) return;
        isDragging = true;
        dragStartX = e.ClientX;
        dragStartY = e.ClientY;
        dragOriginX = posX;
        dragOriginY = posY;
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        if (!isDragging) return;
        posX = dragOriginX + (e.ClientX - dragStartX);
        posY = dragOriginY + (e.ClientY - dragStartY);
    }

    private void OnPointerUp(PointerEventArgs e) => isDragging = false;

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowRight": Next(); break;
            case "ArrowLeft": Prev(); break;
            case "+":
            case "=": ZoomIn(); break;
            case "-":
            case "_": ZoomOut(); break;
            case "0": ResetTransform(); break;
        }
    }

    public ValueTask HandleShortcut(KeyboardKeys actions)
    {
        if (actions == KeyboardKeys.Escape)
        {
            Close();
        }

        return ValueTask.CompletedTask;
    }

    public void Close() => MudDialog?.Close();

    public void Dispose() => IShortcutComponent.Remove(Id);

    // Source-agnostic item the render loop works with. Key is used for @key (stable across rebuilds).
    private sealed record ViewerItem(object Key, string? Name, string? Url, bool IsImage);
}
