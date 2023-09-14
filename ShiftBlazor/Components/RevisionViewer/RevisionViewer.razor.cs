using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class RevisionViewer
    {
        [Inject] internal SettingManager SettingManager { get; set; } = default!;
        [Inject] internal IStringLocalizer<Resources.Components.RevisionViewer> Loc { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance? MudDialog { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter, EditorRequired]
        public string? EntitySet { get; set; }

        internal string UserListBaseUrl { get; set; }
        internal string UserListEntitySet { get; set; }

        protected override void OnInitialized()
        {
            StateHasChanged();
            var url = SettingManager.Configuration.UserListEndpoint;

            if (!string.IsNullOrWhiteSpace(url))
            {
                var index = url.LastIndexOf('/');

                if (index >= 0)
                {
                    UserListBaseUrl = url.Substring(0, index);
                    UserListEntitySet = url.Substring(index + 1);
                }
            }
        }

        private async Task RowClickHandler(DataGridRowClickEventArgs<RevisionDTO> args)
        {
            await Task.Delay(1);
            MudDialog?.Close(args.Item.ValidTo);
        }

        private void Close()
        {
            MudDialog?.Close();
        }
    }
}
