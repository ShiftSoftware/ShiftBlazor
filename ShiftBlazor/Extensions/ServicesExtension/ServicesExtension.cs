using Microsoft.Extensions.DependencyInjection;
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
        var options = new AppStartupOptions();
        configure.Invoke(options);

        services.AddMudServicesWithCustomDialog(mudConfig =>
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
        services.AddScoped<ClipboardService>();
        services.AddScoped<ODataQuery>();
        services.AddScoped<ShiftModal>();
        services.AddScoped<MessageService>();
        services.AddScoped<PrintService>();
        services.AddScoped(x =>
        {
            return new SettingManager(x.GetRequiredService<ILocalStorageService>(),
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


    /// <summary>
    /// Adds common services required by MudBlazor components
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="configuration">Defines options for all MudBlazor services.</param>
    /// <returns>Continues the IServiceCollection chain.</returns>
    public static IServiceCollection AddMudServicesWithCustomDialog(this IServiceCollection services, Action<MudServicesConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new MudServicesConfiguration();
        configuration(options);

        return services
            .AddMudBlazorSnackbar(snackBarConfiguration =>
            {
                snackBarConfiguration.ClearAfterNavigation = options.SnackbarConfiguration.ClearAfterNavigation;
                snackBarConfiguration.MaxDisplayedSnackbars = options.SnackbarConfiguration.MaxDisplayedSnackbars;
                snackBarConfiguration.NewestOnTop = options.SnackbarConfiguration.NewestOnTop;
                snackBarConfiguration.PositionClass = options.SnackbarConfiguration.PositionClass;
                snackBarConfiguration.PreventDuplicates = options.SnackbarConfiguration.PreventDuplicates;
                snackBarConfiguration.MaximumOpacity = options.SnackbarConfiguration.MaximumOpacity;
                snackBarConfiguration.ShowTransitionDuration = options.SnackbarConfiguration.ShowTransitionDuration;
                snackBarConfiguration.VisibleStateDuration = options.SnackbarConfiguration.VisibleStateDuration;
                snackBarConfiguration.HideTransitionDuration = options.SnackbarConfiguration.HideTransitionDuration;
                snackBarConfiguration.ShowCloseIcon = options.SnackbarConfiguration.ShowCloseIcon;
                snackBarConfiguration.RequireInteraction = options.SnackbarConfiguration.RequireInteraction;
                snackBarConfiguration.BackgroundBlurred = options.SnackbarConfiguration.BackgroundBlurred;
                snackBarConfiguration.SnackbarVariant = options.SnackbarConfiguration.SnackbarVariant;
                snackBarConfiguration.IconSize = options.SnackbarConfiguration.IconSize;
                snackBarConfiguration.NormalIcon = options.SnackbarConfiguration.NormalIcon;
                snackBarConfiguration.InfoIcon = options.SnackbarConfiguration.InfoIcon;
                snackBarConfiguration.SuccessIcon = options.SnackbarConfiguration.SuccessIcon;
                snackBarConfiguration.WarningIcon = options.SnackbarConfiguration.WarningIcon;
                snackBarConfiguration.ErrorIcon = options.SnackbarConfiguration.ErrorIcon;
                snackBarConfiguration.HideIcon = options.SnackbarConfiguration.HideIcon;
            })
            .AddMudBlazorResizeListener(resizeOptions =>
            {
                resizeOptions.BreakpointDefinitions = options.ResizeOptions.BreakpointDefinitions;
                resizeOptions.EnableLogging = options.ResizeOptions.EnableLogging;
                resizeOptions.NotifyOnBreakpointOnly = options.ResizeOptions.NotifyOnBreakpointOnly;
                resizeOptions.ReportRate = options.ResizeOptions.ReportRate;
                resizeOptions.SuppressInitEvent = options.ResizeOptions.SuppressInitEvent;
            })
            .AddMudBlazorResizeObserver(observerOptions =>
            {
                observerOptions.EnableLogging = options.ResizeObserverOptions.EnableLogging;
                observerOptions.ReportRate = options.ResizeObserverOptions.ReportRate;
            })
            .AddMudBlazorResizeObserverFactory(observerOptions =>
            {
                observerOptions.EnableLogging = options.ResizeObserverOptions.EnableLogging;
                observerOptions.ReportRate = options.ResizeObserverOptions.ReportRate;
            })
            .AddMudBlazorKeyInterceptor()
            .AddMudBlazorJsEvent()
            .AddMudBlazorScrollManager()
            .AddMudBlazorScrollListener()
            .AddMudBlazorJsApi()
            .AddMudPopoverService(popoverOptions =>
            {
                popoverOptions.CheckForPopoverProvider = options.PopoverOptions.CheckForPopoverProvider;
                popoverOptions.ContainerClass = options.PopoverOptions.ContainerClass;
                popoverOptions.FlipMargin = options.PopoverOptions.FlipMargin;
                popoverOptions.QueueDelay = options.PopoverOptions.QueueDelay;
                popoverOptions.ThrowOnDuplicateProvider = options.PopoverOptions.ThrowOnDuplicateProvider;
                popoverOptions.OverflowPadding = options.PopoverOptions.OverflowPadding;
                //popoverOptions.ModalOverlay = options.PopoverOptions.ModalOverlay;
                //popoverOptions.OverflowBehavior = options.PopoverOptions.OverflowBehavior;
                //popoverOptions.Delay = options.PopoverOptions.Delay;
                //popoverOptions.Duration = options.PopoverOptions.Duration;
            })
            .AddMudBlazorScrollSpy()
            .AddMudBlazorPointerEventsNoneService()
            .AddMudLocalization()
            .AddScoped<IDialogService, ShiftDialogService>();
    }
}
