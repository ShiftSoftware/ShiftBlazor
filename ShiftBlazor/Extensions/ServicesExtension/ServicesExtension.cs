using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftwareLocalization.Blazor;

namespace ShiftSoftware.ShiftBlazor.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddShiftBlazor(this IServiceCollection services, Action<AppStartupOptions> configure)
    {
        services.Configure(configure);
        return services.AddShiftBlazor();
    }

    public static IServiceCollection AddShiftBlazor(this IServiceCollection services)
    {
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<AppStartupOptions>>().Value);

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
        });

        // Apply consumer's MudBlazor customization at resolve time
        services.AddSingleton<IConfigureOptions<MudServicesConfiguration>>(sp =>
            new ConfigureOptions<MudServicesConfiguration>(mudConfig =>
            {
                var appOptions = sp.GetRequiredService<AppStartupOptions>();
                appOptions.MudBlazorConfiguration?.Invoke(mudConfig);
            }));

        services.AddBlazoredLocalStorage();
        services.AddScoped<ClipboardService>();
        services.AddScoped<ODataQuery>();
        services.AddScoped<ShiftModal>();
        services.AddScoped<MessageService>();
        services.AddScoped<PrintService>();
        services.AddScoped(x =>
        {
            var options = x.GetRequiredService<AppStartupOptions>();
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

        services.AddTransient(x =>
        {
            var options = x.GetRequiredService<AppStartupOptions>();
            if (options.LocalizationResource is null)
                return new ShiftBlazorLocalizer(x, typeof(Resource));
            else
                return new ShiftBlazorLocalizer(x, options.LocalizationResource);
        });

        return services;
    }
}
