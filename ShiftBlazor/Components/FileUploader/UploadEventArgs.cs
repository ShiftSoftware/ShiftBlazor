using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components;

public class UploadEventArgs
{
    public List<UploaderItem> Items { get; set; }

    public int Count => Items.Count(x => x.Message == null);
    public int Completed => Items.Count(x => x.State == FileUploadState.Uploaded && x.Message == null);
    public int Failed => Items.Count(x => x.Message != null);

    public UploadEventArgs(List<UploaderItem> items) => Items = items;
}