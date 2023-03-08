using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;
using Syncfusion.Licensing;
using Syncfusion.Blazor;
using Blazored.LocalStorage;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddShiftServices(this IServiceCollection services, Action<ShiftBlazorOptions> configure)
        {
            ShiftBlazorOptions options = new ShiftBlazorOptions();
            configure.Invoke(options);

            services.AddMudServices(mudConfig =>
            {
                mudConfig.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                mudConfig.SnackbarConfiguration.ShowCloseIcon = true;
                mudConfig.SnackbarConfiguration.VisibleStateDuration = 10000;
                mudConfig.SnackbarConfiguration.HideTransitionDuration = 250;
                mudConfig.SnackbarConfiguration.ShowTransitionDuration = 250;
                mudConfig.SnackbarConfiguration.SnackbarVariant = Variant.Text;
                mudConfig.SnackbarConfiguration.BackgroundBlurred = false;
                mudConfig.SnackbarConfiguration.PreventDuplicates = false;
                mudConfig.SnackbarConfiguration.MaxDisplayedSnackbars = 5;

                options.MudBlazorConfiguration?.Invoke(mudConfig);
            });

            services.AddBlazoredLocalStorage();
            services.AddSingleton<ClipboardService>();
            services.AddSingleton(sp => new ODataQuery(options.ODataEndpoint));
            services.AddScoped<ShiftModalService>();
            services.AddScoped<MessageService>();
            services.AddScoped<SettingManager>();

            services.AddSyncfusionBlazor(syncConfig =>
            {
                options.SyncfusionConfiguration?.Invoke(syncConfig);
            });

            SyncfusionLicenseProvider.RegisterLicense(options.SyncfusionLicense);

            return services;
        }

        public class ShiftBlazorOptions
        {
            public string ODataEndpoint { get; set; }
            public string? SyncfusionLicense { get; set; }
            public Action<MudServicesConfiguration> MudBlazorConfiguration { get; set; }
            public Action<GlobalOptions> SyncfusionConfiguration { get; set; }
        }
    }
}
