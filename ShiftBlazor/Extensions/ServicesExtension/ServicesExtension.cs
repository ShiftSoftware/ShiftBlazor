using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftwareLocalization.Blazor;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public static class ServicesExtension
    {
        public static IServiceCollection AddShiftBlazor(this IServiceCollection services, Action<AppStartupOptions> configure)
        {
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

                mudConfig.PopoverOptions.Mode = PopoverMode.Default;

                options.MudBlazorConfiguration?.Invoke(mudConfig);
            });

            services.AddBlazoredLocalStorage();
            services.AddSingleton<ClipboardService>();
            services.AddScoped<ODataQuery>();
            services.AddScoped<ShiftModal>();
            services.AddScoped<MessageService>();
            services.AddScoped<PrintService>();
            services.AddScoped(x =>
            {
                return new SettingManager(x.GetRequiredService<ISyncLocalStorageService>(),
                                   x.GetRequiredService<NavigationManager>(),
                                   x.GetRequiredService<HttpClient>(),
                                   x.GetRequiredService<IJSRuntime>(),
                                   config =>
                                   {
                                       options.ShiftConfiguration?.Invoke(config);
                                   });
            });

            services.AddLocalization();

            if(options.LocalizationResource is null)
                services.AddTransient(x => new ShiftBlazorLocalizer(x, typeof(Resource)));
            else
                services.AddTransient(x => new ShiftBlazorLocalizer(x, options.LocalizationResource));

            return services;
        }
    }
}
