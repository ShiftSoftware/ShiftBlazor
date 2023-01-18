using Microsoft.AspNetCore.Components;
using MudBlazor;
using Syncfusion.Blazor.Grids;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Syncfusion.Blazor.Data;
using ShiftSoftware.ShiftEntity.Core.Dtos;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftList<T, TComponent> : ComponentBase
        where T : ShiftEntityDTOBase, new() 
        where TComponent : ComponentBase
    {
        [Inject] MessageService MsgService { get; set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;
        [Inject] ShiftModalService ShiftModal { get; set; } = default!;

        [CascadingParameter] MudDialogInstance? MudDialog { get; set; }

        [Parameter] public List<T>? Values { get; set; }
        [Parameter] public EventCallback<T> ValueChanged { get; set; }
        [Parameter] public string? Title { get; set; }
        [Parameter] public string? Action { get; set; }
        [Parameter] public int PageSize { get; set; } = 10;
        [Parameter] public List<string> ExcludedHeaders { get; set; } = new();
        [Parameter] public bool EnableCsvExcelExport { get; set; }
        [Parameter] public bool EnablePdfExport { get; set; }
        [Parameter] public bool EnablePrint { get; set; }
        [Parameter] public bool EnableVirtualization { get; set; }
        [Parameter] public bool DisableAdd { get; set; }
        [Parameter] public bool DisablePagination { get; set; }
        [Parameter] public bool DisableSorting { get; set; }
        [Parameter] public bool DisableMultiSorting { get; set; }
        [Parameter] public bool DisableFilters { get; set; }
        [Parameter] public bool DisableSelection { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public RenderFragment? ColumnTemplate { get; set; }
        [Parameter] public RenderFragment? FilterColumnTemplate { get; set; }
        [Parameter] public RenderFragment<T>? ActionsTemplate { get; set; }
        [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
        [Parameter] public Query? Query { get; set; }
        [Parameter] public string ActionColumnWidth { get; set; } = "150";
        [Parameter] public string GridHeight { get; set; } = string.Empty;

        [Parameter] public Dictionary<string, string>? AddDialogParameters { get; set; }
        [Parameter] public bool AutoGenerateColumns { get; set; } = true;
        [Parameter] public bool EmbededInsideForm { get; set; }

        public SfGrid<T>? Grid;
        private readonly PropertyInfo[] Props = typeof(T).GetProperties();
        private CustomMessageHandler MessageHandler = new();
        protected HttpClient HttpClient { get; set; }

        public ShiftList()
        {
            HttpClient = new HttpClient(MessageHandler);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (EmbededInsideForm)
            {
                DisablePagination = true;
            }
        }

        public async Task<DialogResult?> OpenDialog<TT>(object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary< string, string>? parameters = null) where TT : ComponentBase
        {
            var result = await ShiftModal.Open<TT>(key, openMode, parameters);
            
            if (Grid != null)
            {
                await Grid.Refresh();
            }
            return result;
        }

        private void ErrorHandler(FailureEventArgs args)
        {
            MsgService.Error("Error getting list of items", "Error getting list of items", args.Error.ToString());
        }

        public async Task<SelectedItems> GetSelectedItems()
        {
            var AllSelected = await JsRuntime.InvokeAsync<bool>("GridAllSelected", this.Grid!.ID);

            var result = new SelectedItems
            {
                All = AllSelected,
                Query = this.MessageHandler.Query,
            };

            if (!result.All)
            {
                result.Items = this.Grid.SelectedRecords.Select(x => (object)x.ID).ToList();
            }

            return result;
        }

        public async Task AddItem()
        {
            await OpenDialog<TComponent>(null, ModalOpenMode.Popup, this.AddDialogParameters);
        }

        public async Task PrintList()
        {
            if (this.Grid != null)
            {
                MsgService.Info("Opening Print Window, this might take a while");
                await this.Grid.PrintAsync();
                return;
            }
                MsgService.Error("Could not initiate print action");
        }

        public async Task DownloadList(DownloadType type)
        {
            if (this.Grid == null)
            {
                MsgService.Error("Could not initiate download action");
                return;
            }
            switch (type)
            {
                case DownloadType.CSV:
                    await this.Grid.ExportToCsvAsync();
                    break;
                case DownloadType.PDF:
                    await this.Grid.ExportToPdfAsync();
                    break;
                case DownloadType.Excel:
                    await this.Grid.ExportToExcelAsync();
                    break;
            }
        }

        private void CloseDialog()
        {
            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
            }
        }

        public enum DownloadType
        {
            CSV,
            PDF,
            Excel,
        }
    }
}
