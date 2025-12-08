using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class MultiItemField<T> where T : notnull
{
    [CascadingParameter]
    public FormModes? Mode
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                ForceRender();
            }
        }
    }

    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    [Parameter] public List<T> Value { get; set; } = default!;
    [Parameter] public Expression<Func<List<T>>> ValueExpression { get; set; } = default!;
    [Parameter] public EventCallback<List<T>> ValueChanged { get; set; } = default!;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public int Min { get; set; }

    [Parameter]
    public int Max { get; set; }

    [Parameter]
    public bool DisableCounter { get; set; }

    // ========= Classes and Styles Parameters =========

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? GridClass { get; set; }

    [Parameter]
    public string? GridStyle { get; set; }

    [Parameter]
    public string? GridItemClass { get; set; }

    [Parameter]
    public string? GridItemStyle { get; set; }

    // ========= Template Parameters =========
    [Parameter]
    public RenderFragment<MultiItemField<T>>? FieldHeader { get; set; }

    [Parameter]
    public RenderFragment<T>? FieldBody { get; set; }

    [Parameter]
    public RenderFragment<MultiItemField<T>>? FieldFooter { get; set; }

    [Obsolete($"Use {nameof(ControlTemplate)} instead")]
    [Parameter]
    public RenderFragment<RemoveContext>? RemoveButtonTempalte { get; set; }

    [Parameter]
    public RenderFragment<ControlContext>? ControlTemplate { get; set; }

    [Parameter]
    public RenderFragment? TitleTemplate { get; set; }

    [Parameter]
    public bool VerticalControls { get; set; } = true;

    [Parameter]
    public bool DisableDragAndDrop { get; set; } = false;

    [Parameter]
    public bool DisableReorder { get; set; } = false; 

    [Parameter]
    public bool DisableRemove { get; set; }

    [Parameter]
    public Variant ControlVariant { get; set; } = Variant.Outlined;

    private bool UseLimits = false;
    private FieldIdentifier fieldIdentifier;
    private ValidationMessageStore? InternalMessageStore;
    // items added to this list will have a transition applied before removal
    private HashSet<T> ItemsToRemove = [];
    private List<T>? OldValue { get; set; }
    MudDropContainer<T>? DropContainer { get; set; }
    private int DownIndex = -1;
    private int UpIndex = -1;
    private string Classname => new CssBuilder("mt-3")
        .AddClass(Class)
        .Build();

    private string GridClassname => new CssBuilder("sortable-list")
        .AddClass(GridClass)
        .Build();

    private string ItemClassname(int index) =>new CssBuilder("list-item")
        .AddClass("move-up", UpIndex == index || DownIndex != -1 && DownIndex + 1 == index)
        .AddClass("move-down", DownIndex == index || UpIndex - 1 == index)
        .Build();

    private bool PreventRerender { get; set; } = true;
    protected override bool ShouldRender() => !PreventRerender;

    public void ForceRender()
    {
        PreventRerender = false;
        StateHasChanged();
        DropContainer?.Refresh();
        PreventRerender = true;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        try
        {
            parameters.TryGetValue<List<T>?>(nameof(Value), out var newValue);

            // only trigger a re-render if the collection has changed
            if (!new HashSet<T>(OldValue ?? []).SetEquals(newValue ?? []))
            {
                ForceRender();
            }

            OldValue = newValue?.ToList();
        }
        catch { }

        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        UseLimits = ((Max > 0 || Min > 0) && Min <= Max) || (Min > 0 && Max == 0);
        fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        if (UseLimits)
        {
            if (EditContext != null)
            {
                InternalMessageStore = new(EditContext);
                EditContext.OnValidationRequested += (sender, eventArgs) =>
                {
                    InternalMessageStore.Clear();
                    if (Value.Count < Min)
                    {
                        InternalMessageStore.Add(fieldIdentifier, $"At least {Min} items are required.");
                    }
                };
            }

            if (Mode == FormModes.Create)
            {
                var minItemCount = Min;
                var count = Value.Count;

                while (minItemCount-- > count)
                {
                    Value.Add(Activator.CreateInstance<T>());
                }
                ValueChanged.InvokeAsync(Value);
            }
        }

        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // Update List on drag and drop end
            DropContainer?.TransactionEnded += async (sender, args) =>
            {
                var index = args.DestinationIndex;
                if (args.OriginIndex > args.DestinationIndex)
                    index++;

                Value.RemoveAt(args.OriginIndex);
                Value.Insert(index, args.Item!);
                ForceRender();
            };
        }

        base.OnAfterRender(firstRender);
    }

    public void CreateNew()
    {
        InternalMessageStore?.Clear();

        if (!UseLimits || Value.Count < Max || Max == 0)
        {
            Value.Add(Activator.CreateInstance<T>());
            ValueChanged.InvokeAsync(Value);
        }
        ForceRender();
    }

    private async Task MoveUp(T item)
    {
        UpIndex = Value.IndexOf(item);
        if (UpIndex > 0)
        {
            ForceRender();
            await Task.Delay(150);

            Value.RemoveAt(UpIndex);
            Value.Insert(UpIndex - 1, item);
            UpIndex = -1;
            ForceRender();
        }
    }

    private async Task MoveDown(T item)
    {
        DownIndex = Value.IndexOf(item);
        if (DownIndex < Value.Count - 1)
        {
            ForceRender();
            await Task.Delay(150);

            Value.RemoveAt(DownIndex);
            Value.Insert(DownIndex + 1, item);
            DownIndex = -1;
            ForceRender();
        }
    }

    private void AddRemoveTransition(T item)
    {
        ItemsToRemove.Add(item);
        ForceRender();
    }

    public void MarkAllForRemoval()
    {
        foreach (var item in Value.Where(x => x != null))
        {
            ItemsToRemove.Add(item!);
        }
        ForceRender();
    }

    public void Remove(T item)
    {
        if (ItemsToRemove.Contains(item))
        {
            ItemsToRemove.Remove(item);
            Value.Remove(item);
            ValueChanged.InvokeAsync(Value);
        }
    }

    public class RemoveContext
    {
        public T Item { get; set; }
        public Action Remove;

        public RemoveContext(MultiItemField<T> multiItemField, T item)
        {
            Item = item;
            Remove = () => multiItemField.Remove(item!);
        }
    }

    public class ControlContext
    {
        public T Item { get; set; }
        public Action Remove;
        public Action MoveUp;
        public Action MoveDown;
        public Action ForceRerender;
        public Action MarkAllForRemoval;
        public Action MarkForRemoval;

        public ControlContext(MultiItemField<T> multiItemField, T item)
        {
            Item = item;
            Remove = () => multiItemField.Remove(item!);
            MoveUp = () => multiItemField.MoveUp(item).ConfigureAwait(false);
            MoveDown = () => multiItemField.MoveDown(item).ConfigureAwait(false);
            ForceRerender = multiItemField.ForceRender;
            MarkForRemoval = () => multiItemField.AddRemoveTransition(item);
            MarkAllForRemoval = multiItemField.MarkAllForRemoval;
        }
    }
}
