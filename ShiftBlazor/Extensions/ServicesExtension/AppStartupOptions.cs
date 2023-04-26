using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;
using Syncfusion.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShiftSoftware.ShiftBlazor.Extensions.ServicesExtension;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public class AppStartupOptions
    {
        public string? SyncfusionLicense { get; set; }
        public Action<MudServicesConfiguration> MudBlazorConfiguration { get; set; }
        public Action<GlobalOptions> SyncfusionConfiguration { get; set; }
        public Action<AppConfiguration> ShiftConfiguration { get; set; }

    }
}
