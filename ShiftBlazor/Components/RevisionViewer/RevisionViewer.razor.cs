﻿using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
namespace ShiftSoftware.ShiftBlazor.Components
{
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
                    UserListBaseUrl = url[..index];
                    UserListEntitySet = url[(index + 1)..];
                }
            }
        }

        private async Task RowClickHandler(ShiftEvent<DataGridRowClickEventArgs<RevisionDTO>> args)
        {
            await Task.Delay(1);
            MudDialog?.Close(args.Data.Item.ValidTo == DateTime.MaxValue ? null : args.Data.Item.ValidFrom);
        }

        private void Close()
        {
            MudDialog?.Close();
        }
    }
}
