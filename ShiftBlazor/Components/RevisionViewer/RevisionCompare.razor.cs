using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>
/// Shows two revisions side by side as two ordinary forms — the entity's real form, twice, each
/// pinned to its own revision. A banner lists the fields that differ.
/// <para>
/// Each pane renders the hosting form's own <see cref="ChildContent"/>, so the fields in it must
/// bind through the form they are in (<c>context.Item.Foo</c>) rather than through a variable on
/// the page. A page variable is a single object shared by both panes, and both panes would show
/// whatever it happens to hold.
/// </para>
/// </summary>
[CascadingTypeParameter(nameof(T))]
public partial class RevisionCompare<T> : IShortcutComponent where T : ShiftEntityViewAndUpsertDTO, new()
{
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;

    public Guid Id { get; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();

    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

    /// <summary>The form body to render for each revision — the hosting form's own ChildContent.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<FormChildContext<T>>? ChildContent { get; set; }

    /// <summary>Passed through to each pane so it reads the same API as the hosting form.</summary>
    [Parameter, EditorRequired]
    public string Endpoint { get; set; } = default!;

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    /// <summary>The record both panes show; they differ only by revision.</summary>
    [Parameter]
    public object? Key { get; set; }

    /// <summary>The older revision (rendered on the left).</summary>
    [Parameter, EditorRequired]
    public RevisionDTO? OldRevision { get; set; }

    /// <summary>The newer revision (rendered on the right).</summary>
    [Parameter, EditorRequired]
    public RevisionDTO? NewRevision { get; set; }

    [Parameter]
    public string? Title { get; set; }

    /// <summary>Passed through to each pane so read-access gating matches the real form.</summary>
    [Parameter]
    public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

    internal T? OldValue { get; private set; }
    internal T? NewValue { get; private set; }
    internal List<string> ChangedFields { get; private set; } = new();

    internal bool BothLoaded => OldValue != null && NewValue != null;

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new LocalDateTimeOffsetJsonConverter());
        return options;
    }

    protected override void OnInitialized()
    {
        // Register as the top shortcut component so Escape closes this dialog, not the list behind it.
        IShortcutComponent.Register(this);
    }

    /// <summary>
    /// A pane hands over the snapshot it fetched. The banner needs both, so it is recomputed as
    /// each arrives and stays empty until the second one does.
    /// </summary>
    private void OnPaneLoaded(T value, Action<T> capture)
    {
        capture(value);

        if (BothLoaded)
            ChangedFields = ComputeChangedFields(OldValue, NewValue);

        StateHasChanged();
    }

    /// <summary>Display names of the top-level properties that differ between the two snapshots.</summary>
    internal List<string> ComputeChangedFields(T? oldValue, T? newValue)
    {
        var oldNode = JsonSerializer.SerializeToNode(oldValue, SerializerOptions) as JsonObject;
        var newNode = JsonSerializer.SerializeToNode(newValue, SerializerOptions) as JsonObject;

        var keys = new List<string>();
        var seen = new HashSet<string>();
        foreach (var key in NodeKeys(oldNode).Concat(NodeKeys(newNode)))
            if (seen.Add(key))
                keys.Add(key);

        var changed = new List<string>();
        foreach (var key in keys)
        {
            if (IsBookkeeping(key))
                continue;

            var oldChild = oldNode != null && oldNode.TryGetPropertyValue(key, out var o) ? o : null;
            var newChild = newNode != null && newNode.TryGetPropertyValue(key, out var n) ? n : null;

            if (!JsonNode.DeepEquals(oldChild, newChild))
                changed.Add(DisplayNameFor(key));
        }

        return changed;
    }

    /// <summary>
    /// Fields the framework keeps on every entity rather than fields of this record. LastSaveDate
    /// and LastSavedByUserID differ between any two revisions by definition, so listing them says
    /// nothing and pushes the fields that did change further down.
    /// </summary>
    private static bool IsBookkeeping(string jsonKey)
        => typeof(ShiftEntityViewAndUpsertDTO).GetProperty(
               jsonKey,
               BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;

    private static IEnumerable<string> NodeKeys(JsonObject? node)
        => node == null ? Enumerable.Empty<string>() : node.Select(x => x.Key);

    // Web-serialized keys are camelCase; match against properties case-insensitively.
    private static string DisplayNameFor(string jsonKey)
    {
        var prop = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(p.Name, jsonKey, StringComparison.OrdinalIgnoreCase));

        if (prop == null)
            return jsonKey;

        var display = prop.GetCustomAttribute<DisplayAttribute>()?.GetName();
        if (!string.IsNullOrWhiteSpace(display))
            return display!;

        var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName!;

        return prop.Name;
    }

    private static string RevisionDateLabel(RevisionDTO? revision)
    {
        if (revision?.ValidFrom == null)
            return string.Empty;

        return revision.ValidFrom.Value.LocalDateTime.ToString("g");
    }

    internal string OldLabel => Loc["CompareOldRevisionLabel", RevisionDateLabel(OldRevision)];
    internal string NewLabel => Loc["CompareNewRevisionLabel", RevisionDateLabel(NewRevision)];

    // Closing this dialog reveals the revisions list, which stayed open underneath.
    private void Close() => MudDialog?.Close();

    /// <summary>
    /// Escape closes the compare (not the list behind it). Other keys are swallowed while this is
    /// the top shortcut component.
    /// </summary>
    public ValueTask HandleShortcut(KeyboardKeys key)
    {
        if (key == KeyboardKeys.Escape)
            Close();

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        IShortcutComponent.Remove(Id);
    }
}
