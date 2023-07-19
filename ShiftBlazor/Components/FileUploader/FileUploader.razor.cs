using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Extensions;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class FileUploader : Events.EventComponentBase, IDisposable
    {
        [Inject] MessageService MessageService { get; set; } = default!;
        [Inject] HttpClient HttpClient { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;

        [Parameter]
        public List<ShiftFileDTO> Values { get; set; } = new();

        [Parameter]
        public EventCallback<List<ShiftFileDTO>> ValuesChanged { get; set; }

        internal List<UploaderItem> Items { get; set; } = new();

        [Parameter]
        public int MaxFileCount { get; set; } = 16;

        [Parameter]
        public int MaxFileSizeInMegaBytes { get; set; } = 32;

        [Parameter, EditorRequired]
        public string Url { get; set; }

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public string Accept { get; set; } = "";

        [Parameter]
        public bool AcceptImages { get; set; }

        [Parameter]
        public bool ShowThumbnail { get; set; }

        [Parameter]
        public long MaxThumbnailSizeInMegaBytes { get; set; } = 10;

        private FormModes? _mode;

        [CascadingParameter]
        public FormModes? Mode {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode != value)
                {
                    Items = Values.Select(x => new UploaderItem(x)).ToList();
                }
                _mode = value;
            }
        }

        [CascadingParameter]
        public FormTasks? TaskInProgress { get; set; }

        [CascadingParameter(Name = "FormId")]
        public Guid? FormId { get; set; }

        internal string InputId = "Input" + Guid.NewGuid().ToString().Replace("-", string.Empty);
        internal string UploaderId = "Uploader" + Guid.NewGuid().ToString().Replace("-", string.Empty);
        internal string ImageTypes = "image/*";
        internal int ThumbnailSize = 150;

        internal string InputAccept
        {
            get
            {
                var val = Accept;
                if (AcceptImages)
                {
                    val += "," + ImageTypes;
                }
                return val.Trim(',');
            }
        }

        public bool ReadOnly => Mode < FormModes.Edit;
        public bool Disabled => TaskInProgress != null && TaskInProgress != FormTasks.None;

        protected override void OnInitialized()
        {
            OnGridSort += HandleGridSort;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                SetAsSortable();
            }
        }

        private async Task OnInputFileChanged(InputFileChangeEventArgs e)
        {
            if (e.FileCount + Items.Count > MaxFileCount)
            {
                MessageService.Warning($"Max file count for this form is {MaxFileCount}");
                return;
            }

            var files = e.GetMultipleFiles(MaxFileCount);

            Items.AddRange(files.Select(x => new UploaderItem(x)).ToList());

            foreach (var item in Items.Where(x => x.IsWaitingForUpload()))
            {
                await UploadFile(item);
            }

            await SetValue(Items);
        }

        internal async Task UploadFile(UploaderItem item)
        {
            item.File = null;
            item.Message = null;

            var url = SettingManager.Configuration.ApiPath.AddUrlPath(Url);

            using var multipartContent = new MultipartFormDataContent();

            try
            {
                var maxFileSize = MaxFileSizeInMegaBytes * 1024 * 1024;
                var stream = item.LocalFile.OpenReadStream(maxFileSize);

                var content = new StreamContent(stream, Convert.ToInt32(stream.Length));
                content.Headers.ContentType = new MediaTypeHeaderValue(item.LocalFile.ContentType);

                multipartContent.Add(content, "File", item.LocalFile.Name);

                var postResponse = await HttpClient.PostAsync(url, multipartContent);

                try
                {
                    var detail = await postResponse.Content.ReadFromJsonAsync<ShiftEntityResponse<ShiftFileDTO>>();
                    if (postResponse.IsSuccessStatusCode && detail?.Entity != null)
                    {
                        item.File = detail?.Entity;
                    }
                    else
                    {
                        item.Message = detail?.Message;
                    }
                }
                catch (Exception)
                {

                    throw;
                }

            }
            catch
            {
                item.Message = new Message { Title = "Something went wrong" };
            }

            await InvokeAsync(StateHasChanged);
        }

        internal void OpenInput()
        {
            _ = JsRuntime.InvokeVoidAsync("ClickElementById", InputId);
        }

        internal void SetAsSortable()
        {
            _ = JsRuntime.InvokeVoidAsync("SetAsSortable", UploaderId);
        }

        internal async Task Remove(UploaderItem item)
        {
            Items.Remove(item);
            await SetValue(Items);
        }

        internal async void HandleGridSort(object? sender, KeyValuePair<string, List<Guid>> order)
        {
            if (order.Key == UploaderId)
            {
                await SetValue(Items.OrderBy(x => order.Value.IndexOf(x.Id)).ToList());
            }
        }

        internal async Task SetValue(List<UploaderItem> items)
        {
            Values = items.Where(x => x.File != null).Select(x => x.File!).ToList();
            await ValuesChanged.InvokeAsync(Values);
        }

        [JSInvokable]
        public static void ReorderGrid(KeyValuePair<string, List<Guid>> order)
        {
            TriggerGridSort(order);
        }

        void IDisposable.Dispose()
        {
            OnGridSort -= HandleGridSort;
        }
    }
}
