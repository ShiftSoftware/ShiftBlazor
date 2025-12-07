using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Collections;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class MultiItemField<T, TItem> where T : IList
{

    [CascadingParameter]
    public FormModes? Mode { get; set; }

    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    [Parameter] public T Value { get; set; } = default!;
    [Parameter] public Expression<Func<T>> ValueExpression { get; set; } = default!;
    [Parameter] public EventCallback<T> ValueChanged { get; set; } = default!;

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
    public RenderFragment<MultiItemField<T, TItem>>? FieldHeader { get; set; }

    [Parameter]
    public RenderFragment<TItem>? FieldBody { get; set; }

    [Parameter]
    public RenderFragment<MultiItemField<T, TItem>>? FieldFooter { get; set; }

    [Parameter]
    public RenderFragment<RemoveContext>? RemoveButtonTempalte { get; set; }

    [Parameter]
    public RenderFragment? TitleTemplate { get; set; }

    private Type DTOType = typeof(T).GetGenericArguments().First();
    private bool UseLimits = false;
    private FieldIdentifier fieldIdentifier;
    private ValidationMessageStore? InternalMessageStore;

    private string Classname => new CssBuilder("mt-3")
        .AddClass(Class)
        .Build();

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
                    Value.Add(Activator.CreateInstance(DTOType));
                }
                ValueChanged.InvokeAsync(Value);
            }
        }

        base.OnInitialized();
    }

    public void CreateNew()
    {
        InternalMessageStore?.Clear();

        if (!UseLimits || Value.Count < Max || Max == 0)
        {
            Value.Add(Activator.CreateInstance(DTOType));
            ValueChanged.InvokeAsync(Value);
        }
    }

    private void AddRemoveTransition(object item)
    {
        ItemsToRemove.Add(item);
    }

    private HashSet<object> ItemsToRemove = [];

    public void Remove(object item)
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
        public TItem Item { get; set; }
        public Action Remove;

        public RemoveContext(MultiItemField<T, TItem> multiItemField, TItem item)
        {
            Item = item;
            Remove = () => multiItemField.Remove(item!);
        }
    }
}
