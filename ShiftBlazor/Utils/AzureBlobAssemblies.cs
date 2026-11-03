using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ShiftSoftware.ShiftBlazor.Utils;

internal static class AzureBlobAssemblies
{
    private static readonly string[] Names =
    [
        "Apache.Arrow",
        "Azure.Core",
        "Azure.Storage.Blobs",
        "Azure.Storage.Common",
        "Microsoft.Identity.Client",
        "Microsoft.Identity.Client.Extensions.Msal",
        "System.ClientModel",
        "System.IO.Hashing",
        "System.Memory.Data",
        "System.Security.Cryptography.ProtectedData",
    ];

    private static Task? loading;

    public static Task EnsureLoadedAsync(IServiceProvider services)
    {
        var loader = services.GetService<LazyAssemblyLoader>();

        return loader is null ? Task.CompletedTask : loading ??= LoadAsync(loader);
    }

    private static async Task LoadAsync(LazyAssemblyLoader loader)
    {
        try
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).ToHashSet();
            var missing = Names.Where(x => !loaded.Contains(x)).Select(x => x + ".wasm").ToList();

            if (missing.Count > 0)
                await loader.LoadAssembliesAsync(missing);
        }
        catch
        {
            loading = null;
            throw;
        }
    }
}
