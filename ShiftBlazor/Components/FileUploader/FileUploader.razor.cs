using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class FileUploader : Events.EventComponentBase, IDisposable
{
    [Inject] internal MessageService MessageService { get; set; } = default!;
    [Inject] HttpClient HttpClient { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<ShiftFileDTO>? Values { get; set; }

    [Parameter]
    public EventCallback<List<ShiftFileDTO>?> ValuesChanged { get; set; }

    internal List<UploaderItem> Items { get; set; } = new();

    [Parameter]
    public int MaxFileCount { get; set; } = 16;

    [Parameter]
    public int MaxFileSizeInMegaBytes { get; set; } = 32;

    [Parameter]
    public string? Url { get; set; } = "/AzureStorage/generate-file-upload-sas";

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

    [Parameter]
    public string? AccountName { get; set; }

    [Parameter]
    public string? ContainerName { get; set; }

    [Parameter]
    public bool? DisableToolbar { get; set; }

    [Parameter]
    public string? DropAreaSelector { get; set; }

    [Parameter]
    public string? Prefix { get; set; }

    [Parameter]
    public bool? HideUI { get; set; }

    [CascadingParameter(Name = "ShiftForm")]
    public IShiftForm? ShiftForm { get; set; }

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
                Items = Values?.Select(x => new UploaderItem(x)).ToList() ?? new();
            }
            _mode = value;
        }
    }

    internal string InputId = "Input" + Guid.NewGuid().ToString().Replace("-", string.Empty);
    internal string UploaderId = "Uploader" + Guid.NewGuid().ToString().Replace("-", string.Empty);
    internal string ImageTypes = "image/*";
    internal int ThumbnailSize = 150;
    internal bool _ShowThumbnail;
    private string InputStyle = "position: absolute;top: 0;left: 0;height: 100%;width: 100%;z-index:1000;display: none;opacity: 0;";

    [Inject] internal TypeAuth.Core.ITypeAuthService TypeAuthService { get; set; } = default!;
    [Parameter]
    public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }
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

    internal string Classes {
        get
        {
            var classNames = "FileUpload relative my-4 z-10";
            if (ReadOnly) classNames += " readonly";
            return classNames;
        }
    }

    internal string FieldLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Label))
            {
                var label = Label;
                if (For != null && FormHelper.IsRequired(For)) label += "*";
                return label;
            }

            return string.Empty;
        }
    }

    public bool ReadOnly => Mode < FormModes.Edit;
    public bool Disabled => ShiftForm?.TaskInProgress != null && ShiftForm.TaskInProgress != FormTasks.None;

    [Parameter]
    public Expression<Func<List<ShiftFileDTO>>>? For { get; set; }

    private FieldIdentifier _FieldIdentifier;
    private string? ErrorText;

    protected override void OnInitialized()
    {
        _ShowThumbnail = ShowThumbnail;

        OnGridSort += HandleGridSort;

        if (For != null && ShiftForm?.EditContext != null)
        {
            _FieldIdentifier = FieldIdentifier.Create(For);
            ShiftForm.EditContext.OnValidationStateChanged += (o, args) =>
            {
                ErrorText = ShiftForm.EditContext.GetValidationMessages(_FieldIdentifier).FirstOrDefault();
            };
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SetAsSortable();
            SetDropZone();
        }
    }

    void ToggleViewMode()
    {
        _ShowThumbnail = !_ShowThumbnail;
    }

    void ViewGallery()
    {
        if (Items.Count < 1) { return; }

        var options = new DialogOptions
        {
            NoHeader = true,
            BackdropClick = true,
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.False,
        };

        var parameters = new DialogParameters
        {
            { "Files", Items },
        };

        DialogService.Show<ImageViewer>("", parameters, options);
    }

    private async Task OnInputFileChanged(InputFileChangeEventArgs e)
    {
        if (e.FileCount + Items.Count > MaxFileCount)
        {
            MessageService.Warning(Loc["MaxAllowedFileUploadWarning", MaxFileCount]);
            return;
        }

        var files = e.GetMultipleFiles(MaxFileCount);

        Items.AddRange(files.Select(x => new UploaderItem(x)).ToList());

        var filesToUpload = Items.Where(x => x.IsWaitingForUpload()).ToList();

        await GetSASForFilesAsync(filesToUpload);

        var tasks = new List<Task>();

        foreach (var item in filesToUpload)
        {
            tasks.Add(Task.Run(async () => await UploadFileToAzureAsync(item)));
        }

        await Task.WhenAll(tasks);

        await SetValue(Items);
    }

    internal async Task GetSASForFilesAsync(IEnumerable<UploaderItem> items)
    {
        var url = SettingManager.Configuration.ApiPath.AddUrlPath(Url);

        List<ShiftFileDTO> files = new();

        foreach (var item in items)
        {
            if (item.LocalFile == null)
            {
                item.Message = new Message { Title = Loc["FileUploaderError1"] };
                return;
            }

            item.File = null;
            item.Message = null;

            var fileName = Uri.EscapeDataString(item.LocalFile.Name);
            var file = new ShiftFileDTO
            {
                Blob = string.IsNullOrWhiteSpace(Prefix) ? fileName : Prefix.AddUrlPath(fileName),
                Name = item.LocalFile.Name
            };

            if (!string.IsNullOrWhiteSpace(AccountName))
                file.AccountName = AccountName;

            if (!string.IsNullOrWhiteSpace(ContainerName))
                file.ContainerName = ContainerName;

            files.Add(file);
        }

        var postResponse = await HttpClient.PostAsJsonAsync(url, files);

        try
        {
            var filesWithSASTokenReponse = await postResponse.Content.ReadFromJsonAsync<ShiftEntityResponse<List<ShiftFileDTO>>>();

            if (postResponse.IsSuccessStatusCode && filesWithSASTokenReponse?.Entity != null)
            {
                filesWithSASTokenReponse.Entity.ForEach((file) =>
                {
                    var match = items.FirstOrDefault(x => x.LocalFile?.Name == file.Name)!;

                    match.File = file;
                });
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    internal async Task UploadFile(UploaderItem item)
    {
        if (item.LocalFile == null)
        {
            item.Message = new Message { Title = Loc["FileUploaderError1"] };
            return;
        }

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

            if (!string.IsNullOrWhiteSpace(AccountName))
                multipartContent.Headers.Add("Account-Name", AccountName);

            if (!string.IsNullOrWhiteSpace(ContainerName))
                multipartContent.Headers.Add("Container-Name", ContainerName);

            var postResponse = await HttpClient.PostAsync(url, multipartContent, item.CancellationTokenSource!.Token);

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
            item.Message = new Message { Title = Loc["FileUploaderError2"] };
            item.CancellationTokenSource?.Cancel();
        }

        await InvokeAsync(StateHasChanged);
    }

    internal async Task UploadFileToAzureAsync(UploaderItem item)
    {
        if (item.LocalFile == null)
        {
            item.Message = new Message { Title = Loc["FileUploaderError1"] };
            return;
        }

        item.Message = null;

        try
        {
            var maxFileSize = MaxFileSizeInMegaBytes * 1024 * 1024;
            var stream = item.LocalFile.OpenReadStream(maxFileSize);

            var blobClient = new BlobClient(new Uri(item.File!.Url!));

            item.File.ContentType = item.LocalFile.ContentType;

            item.File.Size = item.LocalFile.Size;

            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = item.LocalFile.ContentType });

            item.LocalFile = null;
        }
        catch (Exception ex)
        {
            item.Message = new Message { Title = Loc["FileUploaderError3", ex.Message] };
            item.CancellationTokenSource?.Cancel();
        }

        await InvokeAsync(StateHasChanged);
    }

    internal async Task OpenInput(string? id = null, bool? directoryUpload = false)
    {
        await JsRuntime.InvokeVoidAsync("openInput", id ?? InputId, directoryUpload);
    }

    internal void SetAsSortable()
    {
        _ = JsRuntime.InvokeVoidAsync("SetAsSortable", UploaderId);
    }

    internal async Task Remove(UploaderItem item)
    {
        if (TypeAuthAction is null || TypeAuthService.Can(TypeAuthAction, TypeAuth.Core.Access.Delete))
        {
            item.CancellationTokenSource?.Cancel();
            Items.Remove(item);
            await SetValue(Items);
        }
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
        var _values = items.Where(x => x.File != null).Select(x => x.File!).ToList();
        Values = _values.Count > 0 ? _values : null;

        ErrorText = null;
        await ValuesChanged.InvokeAsync(Values);
    }

    internal async Task DownloadFile(ShiftFileDTO file)
    {
        await JsRuntime.InvokeVoidAsync("downloadFileFromUrl", file.Name, file.Url);
    }

    public async Task ClearAll()
    {
        if (Mode >= FormModes.Edit)
        {
            foreach (var item in Items.Where(x => x.CancellationTokenSource != null))
            {
                item.CancellationTokenSource!.Cancel();
            };
            Items.Clear();
            await SetValue([]);
        }
    }

    private void SetDropZone()
    {
        _ = JsRuntime.InvokeVoidAsync(
                "setDropZone",
                $"#" + UploaderId,
                DropAreaSelector
            );
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
