using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public class AppStartupOptions
    {
        public Action<MudServicesConfiguration> MudBlazorConfiguration { get; set; }
        public Action<AppConfiguration> ShiftConfiguration { get; set; }

    }
}
