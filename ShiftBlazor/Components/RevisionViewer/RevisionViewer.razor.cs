using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class RevisionViewer
{
    [Inject] internal SettingManager SettingManager { get; set; } = default!;
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] internal HttpClient HttpClient { get; set; } = default!;
    [Inject] internal ISnackbar Snackbar { get; set; } = default!;

    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter, EditorRequired]
    public string? EntitySet { get; set; }

    /// <summary>
    ///     Absolute URL of the entity record, is used for compare feature.
    ///     When null, compare is hidden.
    /// </summary>
    [Parameter]
    public string? ItemUrl { get; set; }

    internal string? UserListBaseUrl { get; set; }
    internal string? UserListEntitySet { get; set; }

    internal record CompareRow(string Field, string? Old, string? New, bool Changed);

    internal bool CompareEnabled => !string.IsNullOrWhiteSpace(ItemUrl);
    // we need to use a custom select column because the ShiftList Select column wouldn't work here
    // the ShiftList uses ID to select but we don't have IDs here.
    internal List<RevisionDTO> SelectedRevisions { get; } = new();
    internal bool ShowCompare { get; set; }
    internal bool CompareLoading { get; set; }
    internal bool OnlyChanges { get; set; } = true;
    internal List<CompareRow> CompareRows { get; set; } = new();

    internal bool CanCompare => CompareEnabled && SelectedRevisions.Count == 2 && !CompareLoading;

    protected override void OnInitialized()
    {
        StateHasChanged();
        var url = SettingManager.Configuration.UserListEndpoint;

        if (!string.IsNullOrWhiteSpace(url))
        {
            var index = url.LastIndexOf('/');

            if (index >= 0)
            {
                UserListBaseUrl = url[..index];
                UserListEntitySet = url[(index + 1)..];
            }
        }
    }

    internal bool IsSelected(RevisionDTO revision)
        => SelectedRevisions.Any(x => x.ValidFrom == revision.ValidFrom);

    private void ToggleSelect(RevisionDTO revision)
    {
        var existing = SelectedRevisions.FirstOrDefault(x => x.ValidFrom == revision.ValidFrom);

        if (existing != null)
            SelectedRevisions.Remove(existing);
        else
            SelectedRevisions.Add(revision);

        StateHasChanged();
    }

    private async Task RowClickHandler(ShiftEvent<DataGridRowClickEventArgs<RevisionDTO>> args)
    {
        await Task.Delay(1);
        MudDialog?.Close(args.Data.Item.ValidTo == DateTime.MaxValue ? null : args.Data.Item.ValidFrom);
    }

    internal async Task CompareHandler()
    {
        if (!CanCompare)
            return;

        CompareLoading = true;
        StateHasChanged();

        try
        {
            // Older revision is "Old", newer is "New".
            var ordered = SelectedRevisions
                .OrderBy(x => x.ValidFrom ?? DateTimeOffset.MinValue)
                .ToList();

            var oldEntity = await FetchSnapshot(ordered[0]);
            var newEntity = await FetchSnapshot(ordered[1]);

            CompareRows = BuildDiff(oldEntity, newEntity);
            ShowCompare = true;
        }
        catch (Exception e)
        {
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            CompareLoading = false;
            StateHasChanged();
        }
    }

    private void BackToList()
    {
        ShowCompare = false;
        CompareRows = new();
        StateHasChanged();
    }

    private async Task<JsonObject?> FetchSnapshot(RevisionDTO revision)
    {
        // A live revision (ValidTo == MaxValue) has no meaningful asOf; fetch the current record.
        var asOf = revision.ValidFrom;
        var url = ItemUrl;

        if (asOf != null && revision.ValidTo != DateTime.MaxValue)
            url += "?asOf=" + Uri.EscapeDataString(asOf.Value.ToString("O"));

        using var request = HttpClient.CreateRequestMessage(HttpMethod.Get, new Uri(url!));
        using var res = await HttpClient.SendAsync(request);
        res.EnsureSuccessStatusCode();

        var response = await res.Content.ReadFromJsonAsync<ShiftEntityResponse<JsonObject>>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return response?.Entity;
    }

    internal List<CompareRow> BuildDiff(JsonObject? oldEntity, JsonObject? newEntity)
    {
        var fields = new List<string>();
        var seen = new HashSet<string>();

        foreach (var key in EntityKeys(oldEntity).Concat(EntityKeys(newEntity)))
            if (seen.Add(key))
                fields.Add(key);

        var rows = new List<CompareRow>();

        foreach (var field in fields)
        {
            var oldValue = FormatValue(oldEntity, field);
            var newValue = FormatValue(newEntity, field);
            rows.Add(new CompareRow(field, oldValue, newValue, !string.Equals(oldValue, newValue, StringComparison.Ordinal)));
        }

        // Changed rows first, otherwise keep declared order.
        return rows.OrderByDescending(x => x.Changed).ToList();
    }

    private static IEnumerable<string> EntityKeys(JsonObject? entity)
        => entity == null ? Enumerable.Empty<string>() : entity.Select(x => x.Key);

    private string FormatValue(JsonObject? entity, string field)
    {
        if (entity == null || !entity.TryGetPropertyValue(field, out var node) || node == null)
            return Loc["CompareEmptyValue"];

        // Objects and arrays are compared/shown as compact JSON; primitives as their string form.
        return node is JsonValue value ? value.ToString() : node.ToJsonString();
    }

    private void Close()
    {
        MudDialog?.Close();
    }
}
