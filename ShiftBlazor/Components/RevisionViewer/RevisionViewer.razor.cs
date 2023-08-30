using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Syncfusion.Blazor.Grids;
namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class RevisionViewer
    {
        [Inject] internal ShiftModal ShiftModal { get; set; } = default!;
        [Inject] internal SettingManager SettingManager { get; set; } = default!;
        [Inject] internal IStringLocalizer<Resources.Components.RevisionViewer> Loc { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance? MudDialog { get; set; }

        [Parameter]
        [EditorRequired]
        public List<RevisionDTO> Revisions { get; set; } = new();

        [Parameter]
        public string? Title { get; set; }

        internal string BaseUrl { get; set; }
        internal string EntitySet { get; set; }

        protected override void OnInitialized()
        {
            StateHasChanged();
            var url = SettingManager.Configuration.UserListEndpoint;

            if (!string.IsNullOrWhiteSpace(url))
            {
                var index = url.LastIndexOf('/');

                if (index >= 0)
                {
                    BaseUrl = url.Substring(0, index);
                    EntitySet = url.Substring(index + 1);
                }
            }
        }

        private async Task RowClickHandler(RecordClickEventArgs<RevisionDTO> args)
        {
            await Task.Delay(1);
            MudDialog?.Close(args.RowData.ValidTo);
        }

        private void Close()
        {
            MudDialog?.Close();
        }
    }
}
