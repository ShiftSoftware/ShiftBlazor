using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;
using Syncfusion.Licensing;
using Syncfusion.Blazor;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public static class ServicesExtension
    {
        public static IServiceCollection AddShiftServices(this IServiceCollection services, Action<AppStartupOptions> configure)
        {
            SettingManager settingManager = null;
            var options = new AppStartupOptions();
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
            services.AddScoped<ODataQuery>();
            services.AddScoped<ShiftModal>();
            services.AddScoped<MessageService>();
            services.AddScoped(x =>
            {
                settingManager = new SettingManager(x.GetRequiredService<ISyncLocalStorageService>(),
                                   x.GetRequiredService<NavigationManager>(),
                                   x.GetRequiredService<HttpClient>(),
                                   x.GetRequiredService<IJSRuntime>(),
                                   config =>
                                   {
                                       options.ShiftConfiguration.Invoke(config);
                                   });

                return settingManager;
            });

            SyncfusionLicenseProvider.RegisterLicense(options.SyncfusionLicense);

            services.AddSyncfusionBlazor(syncConfig =>
            {
                syncConfig.EnableRtl = settingManager?.Settings.CurrentLanguage?.RTL ?? false;
                options.SyncfusionConfiguration?.Invoke(syncConfig);
            });

            services.AddLocalization();

            return services;
        }
    }
}
