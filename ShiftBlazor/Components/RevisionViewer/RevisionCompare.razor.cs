using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>
/// Shows two revisions side by side, read-only, using the entity's real form fields so values
/// look as they do in the form. A banner lists the fields that differ.
/// </summary>
[CascadingTypeParameter(nameof(T))]
public partial class RevisionCompare<T> : IShortcutComponent where T : ShiftEntityViewAndUpsertDTO, new()
{
    [Inject] internal HttpClient HttpClient { get; set; } = default!;
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] internal ISnackbar Snackbar { get; set; } = default!;

    public Guid Id { get; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();

    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

    /// <summary>The form body to render for each revision — the hosting form's own ChildContent.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<FormChildContext<T>>? ChildContent { get; set; }

    /// <summary>Absolute URL of the entity record; snapshots are fetched from it with <c>?asOf=</c>.</summary>
    [Parameter, EditorRequired]
    public string? ItemUrl { get; set; }

    /// <summary>The older revision (rendered on the left).</summary>
    [Parameter, EditorRequired]
    public RevisionDTO? OldRevision { get; set; }

    /// <summary>The newer revision (rendered on the right).</summary>
    [Parameter, EditorRequired]
    public RevisionDTO? NewRevision { get; set; }

    [Parameter]
    public string? Title { get; set; }

    /// <summary>Passed through to each host so read-access gating matches the real form.</summary>
    [Parameter]
    public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

    internal T? OldValue { get; private set; }
    internal T? NewValue { get; private set; }
    internal bool Loading { get; private set; } = true;
    internal List<string> ChangedFields { get; private set; } = new();

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

    protected override async Task OnInitializedAsync()
    {
        try
        {
            OldValue = await FetchSnapshot(OldRevision);
            NewValue = await FetchSnapshot(NewRevision);
            ChangedFields = ComputeChangedFields(OldValue, NewValue);
        }
        catch (Exception e)
        {
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            Loading = false;
        }
    }

    private async Task<T?> FetchSnapshot(RevisionDTO? revision)
    {
        if (revision == null)
            return null;

        // A live revision (ValidTo == MaxValue) has no meaningful asOf; fetch the current record.
        var asOf = revision.ValidFrom;
        var url = ItemUrl;

        if (asOf != null && revision.ValidTo != DateTime.MaxValue)
            url += "?asOf=" + Uri.EscapeDataString(asOf.Value.ToString("O"));

        using var request = HttpClient.CreateRequestMessage(HttpMethod.Get, new Uri(url!));
        using var res = await HttpClient.SendAsync(request);
        res.EnsureSuccessStatusCode();

        var response = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<T>>(SerializerOptions);
        return response?.Entity;
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
            var oldChild = oldNode != null && oldNode.TryGetPropertyValue(key, out var o) ? o : null;
            var newChild = newNode != null && newNode.TryGetPropertyValue(key, out var n) ? n : null;

            if (!JsonNode.DeepEquals(oldChild, newChild))
                changed.Add(DisplayNameFor(key));
        }

        return changed;
    }

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
