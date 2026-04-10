using System.Reflection;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Extensions;

public class AppStartupOptions
{
    public Action<MudServicesConfiguration>? MudBlazorConfiguration { get; set; }
    public Action<AppConfiguration>? ShiftConfiguration { get; set; }
    public Type? LocalizationResource { get; set; }
    public List<Assembly> AdditionalAssemblies { get; } = new();

    public AppStartupOptions AddAssembly(Assembly assembly)
    {
        if (!AdditionalAssemblies.Contains(assembly))
            AdditionalAssemblies.Add(assembly);

        return this;
    }
}
