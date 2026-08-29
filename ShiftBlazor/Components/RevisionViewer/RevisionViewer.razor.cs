using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class RevisionViewer
{
    [Inject] internal SettingManager SettingManager { get; set; } = default!;
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;

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

    /// <summary>Raised when the user compares the two selected revisions; the caller renders the compare view.</summary>
    [Parameter]
    public EventCallback<CompareRevisions> OnCompareRequested { get; set; }

    /// <summary>
    ///     Raised when the user opens one revision in its own browser tab; the caller owns the URL.
    ///     When unset, the per-row button is hidden.
    /// </summary>
    [Parameter]
    public EventCallback<RevisionDTO> OnOpenInNewTabRequested { get; set; }

    internal string? UserListBaseUrl { get; set; }
    internal string? UserListEntitySet { get; set; }

    internal bool CompareEnabled => !string.IsNullOrWhiteSpace(ItemUrl);
    internal bool NewTabEnabled => OnOpenInNewTabRequested.HasDelegate;
    // we need to use a custom select column because the ShiftList Select column wouldn't work here
    // the ShiftList uses ID to select but we don't have IDs here.
    internal List<RevisionDTO> SelectedRevisions { get; } = new();

    internal bool CanCompare => CompareEnabled && SelectedRevisions.Count == 2;

    private readonly Dictionary<string, SortDirection> _sort = new ()
    {
        { nameof(RevisionDTO.ValidFrom), SortDirection.Descending }
    };

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
        // The current revision is the live record (no asOf); anything else is historical.
        MudDialog?.Close(DialogResult.Ok(new ViewRevision(args.Data.Item.AsOf())));
    }

    internal async Task CompareHandler()
    {
        if (!CanCompare)
            return;

        // Oldest first, so Old/New are decided by date rather than selection order.
        var ordered = SelectedRevisions
            .OrderBy(x => x.ValidFrom ?? DateTimeOffset.MinValue)
            .ToList();

        await OnCompareRequested.InvokeAsync(new CompareRevisions(ordered[0], ordered[1]));
    }

    private void Close()
    {
        MudDialog?.Close();
    }
}
