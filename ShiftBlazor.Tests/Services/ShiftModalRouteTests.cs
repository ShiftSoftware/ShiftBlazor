using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Tests.Services;

public class ShiftModalRouteTests : ShiftBlazorTestContext
{
    [Fact]
    public async Task Shared_route_form_writes_an_exact_identifier_and_reopens_after_refresh()
    {
        var (modal, js, shown) = CreateHarness();

        await modal.Open(typeof(SharedFormProbe), "contact-7", parameters: new Dictionary<string, object>
        {
            ["Revision"] = 4
        });

        var entry = Assert.Single(modal.ParseModalUrl(js.CurrentUrl));
        Assert.Equal("modal-shared/{Key?}", entry.Name);
        Assert.Equal("contact-7", entry.Key?.ToString());
        Assert.Equal("4", entry.Parameters?["Revision"]?.ToString());
        shown.Clear();

        await modal.UpdateModals();

        var reopened = Assert.Single(shown);
        Assert.Equal(typeof(SharedFormProbe), reopened.ComponentType);
        Assert.Equal("contact-7", reopened.Parameters["Key"]);
        Assert.Equal(4, reopened.Parameters["Revision"]);
    }

    [Fact]
    public async Task Keyed_legacy_collision_selects_form_even_if_list_was_last()
    {
        var (modal, js, shown) = CreateHarness();
        js.SetModals(new ModalInfo { Name = "modal-list-wins", Key = "contact-7" });

        await modal.UpdateModals();

        Assert.Equal(typeof(FormBeforeListProbe), Assert.Single(shown).ComponentType);
        Assert.Equal("contact-7", shown[0].Parameters["Key"]);
    }

    [Fact]
    public async Task Keyless_legacy_collision_preserves_the_previous_list_winner()
    {
        var (modal, js, shown) = CreateHarness();
        js.SetModals(new ModalInfo { Name = "modal-list-wins" });

        await modal.UpdateModals();

        Assert.Equal(typeof(ListAfterFormProbe), Assert.Single(shown).ComponentType);
    }

    [Fact]
    public void Legacy_names_with_two_keyed_routes_keep_the_previous_discovery_order()
    {
        var modal = Services.GetRequiredService<ShiftModal>();
        var previousWinner = typeof(ShiftModalRouteTests).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>()
                .Any(route => route.Template.StartsWith("/modal-compat/", StringComparison.Ordinal)))
            .Last();

        Assert.Equal(previousWinner, modal.GetComponentType("modal-compat"));
        Assert.Equal(typeof(NumericKeyProbe), modal.GetComponentType("modal-compat/{Key:int?}"));
        Assert.Equal(typeof(GuidKeyProbe), modal.GetComponentType("modal-compat/{Key:guid?}"));
    }

    [Fact]
    public async Task Opening_a_legacy_shared_path_with_a_key_writes_an_unambiguous_url()
    {
        var (modal, js, shown) = CreateHarness();

        await modal.Open("modal-list-wins", "contact-7");

        Assert.Equal(typeof(FormBeforeListProbe), Assert.Single(shown).ComponentType);
        Assert.Equal("modal-list-wins/{Key?}", Assert.Single(modal.ParseModalUrl(js.CurrentUrl)).Name);
    }

    [Fact]
    public async Task Distinct_legacy_routes_reopen_with_prefix_key_and_typed_parameters()
    {
        var (modal, js, shown) = CreateHarness();
        js.SetModals(new ModalInfo
        {
            Name = "identity/modal-distinct-form",
            Key = "user-7",
            Parameters = new Dictionary<string, object> { ["Revision"] = 4 }
        });

        await modal.UpdateModals();

        var reopened = Assert.Single(shown);
        Assert.Equal(typeof(DistinctFormProbe), reopened.ComponentType);
        Assert.Equal("user-7", reopened.Parameters["Key"]);
        Assert.Equal(4, reopened.Parameters["Revision"]);
        Assert.Equal(typeof(DistinctListProbe), modal.GetComponentType("identity/modal-distinct-list"));
    }

    [Fact]
    public async Task New_distinct_form_link_and_old_name_both_open_in_a_normal_new_tab_url()
    {
        var (modal, js, shown) = CreateHarness();
        await modal.Open(typeof(DistinctFormProbe), "user-7");
        var entry = Assert.Single(modal.ParseModalUrl(js.CurrentUrl));
        Assert.Equal("identity/modal-distinct-form/{Key?}", entry.Name);
        shown.Clear();

        await modal.UpdateModals();
        Assert.Equal(typeof(DistinctFormProbe), Assert.Single(shown).ComponentType);

        await modal.Open("/identity/modal-distinct-form", "user-7", ModalOpenMode.NewTab);
        Assert.Equal("http://localhost/identity/modal-distinct-form/user-7", js.OpenedUrl);
        await modal.Open(entry.Name, "user-7", ModalOpenMode.NewTab);
        Assert.Equal("http://localhost/identity/modal-distinct-form/user-7", js.OpenedUrl);
    }

    [Fact]
    public async Task Stacked_legacy_and_new_links_reopen_in_url_order()
    {
        var (modal, js, shown) = CreateHarness();
        js.SetModals(
            new ModalInfo { Name = "identity/modal-distinct-form", Key = "user-7" },
            new ModalInfo { Name = "modal-shared/{Key?}", Key = "contact-7" });

        await modal.UpdateModals();

        Assert.Equal(new[] { typeof(DistinctFormProbe), typeof(SharedFormProbe) },
            shown.Select(x => x.ComponentType));
    }

    [Fact]
    public async Task Static_and_unrouted_dialogs_keep_their_existing_url_behavior()
    {
        var (modal, js, shown) = CreateHarness();

        await modal.Open(typeof(DistinctListProbe));
        Assert.Equal("identity/modal-distinct-list", Assert.Single(modal.ParseModalUrl(js.CurrentUrl)).Name);
        Assert.Equal(typeof(DistinctListProbe), Assert.Single(shown).ComponentType);

        var (unroutedModal, unroutedJs, unroutedShown) = CreateHarness();
        await unroutedModal.Open(typeof(UnroutedDialogProbe), parameters: new Dictionary<string, object>
        {
            ["Revision"] = 4
        });
        Assert.Empty(unroutedModal.ParseModalUrl(unroutedJs.CurrentUrl));
        Assert.Equal(typeof(UnroutedDialogProbe), Assert.Single(unroutedShown).ComponentType);
        Assert.Equal(4, unroutedShown[0].Parameters["Revision"]);
    }

    [Fact]
    public void Aliases_and_case_insensitive_route_lookup_still_work()
    {
        var modal = Services.GetRequiredService<ShiftModal>();

        Assert.Equal(typeof(AliasFormProbe), modal.GetComponentType("modal-route-alias/{Key?}"));
        Assert.Equal(typeof(AliasFormProbe), modal.GetComponentType("modal-route-primary/{Key?}"));
        Assert.Equal(typeof(AliasFormProbe), modal.GetComponentType("modal-route-alias"));
        Assert.Equal(typeof(AliasFormProbe), modal.GetComponentType("modal-route-primary"));
        Assert.Equal(typeof(DistinctListProbe), modal.GetComponentType("/IDENTITY/MODAL-DISTINCT-LIST"));
    }

    private (ShiftModal Modal, RecordingJsRuntime Js, List<(Type ComponentType, DialogParameters Parameters)> Shown)
        CreateHarness()
    {
        var shown = new List<(Type, DialogParameters)>();
        var js = new RecordingJsRuntime("http://localhost/sample-contact-list");
        var dialogs = DispatchProxy.Create<IDialogService, DialogServiceProxy>();
        ((DialogServiceProxy)(object)dialogs).Shown = shown;
        var modal = new ShiftModal(js, Services.GetRequiredService<NavigationManager>(), dialogs,
            Services.GetRequiredService<SettingManager>(), Services.GetRequiredService<MessageService>());
        return (modal, js, shown);
    }

    public class DialogServiceProxy : DispatchProxy
    {
        public List<(Type ComponentType, DialogParameters Parameters)> Shown { get; set; } = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "ShowAsync" && args is [Type componentType, _, DialogParameters parameters, _])
            {
                Shown.Add((componentType, parameters));
                return Task.FromResult(DispatchProxy.Create<IDialogReference, DialogReferenceProxy>());
            }

            throw new NotSupportedException(method?.Name);
        }
    }

    public class DialogReferenceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Result")
                return Task.FromResult<DialogResult?>(DialogResult.Cancel());

            throw new NotSupportedException(method?.Name);
        }
    }

    private sealed class RecordingJsRuntime(string currentUrl) : IJSRuntime
    {
        public string CurrentUrl { get; private set; } = currentUrl;
        public string? OpenedUrl { get; private set; }

        public void SetModals(params ModalInfo[] modals)
        {
            var encoded = Uri.EscapeDataString(JsonSerializer.Serialize(modals));
            CurrentUrl = $"http://localhost/sample-contact-list?modal={encoded}";
        }

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
            => ValueTask.FromResult((TValue)(object)CurrentUrl);

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken)
            => GetValueAsync<TValue>(identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "history.pushState")
                CurrentUrl = new Uri(new Uri(CurrentUrl), (string)args![2]!).ToString();
            else if (identifier == "open")
                OpenedUrl = (string)args![0]!;
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    [Route("/modal-shared")]
    private sealed class SharedListProbe : ComponentBase { }

    [Route("/modal-shared/{Key?}")]
    private sealed class SharedFormProbe : ComponentBase
    {
        [Parameter] public string? Key { get; set; }
        [Parameter] public int Revision { get; set; }
    }

    [Route("/modal-list-wins/{Key?}")]
    private sealed class FormBeforeListProbe : ComponentBase
    {
        [Parameter] public string? Key { get; set; }
    }

    [Route("/modal-list-wins")]
    private sealed class ListAfterFormProbe : ComponentBase { }

    [Route("/identity/modal-distinct-list")]
    private sealed class DistinctListProbe : ComponentBase { }

    [Route("/identity/modal-distinct-form/{Key?}")]
    private sealed class DistinctFormProbe : ComponentBase
    {
        [Parameter] public string? Key { get; set; }
        [Parameter] public int Revision { get; set; }
    }

    [Route("/modal-route-alias/{Key?}")]
    [Route("/modal-route-primary/{Key?}")]
    private sealed class AliasFormProbe : ComponentBase
    {
        [Parameter] public string? Key { get; set; }
    }

    [Route("/modal-compat/{Key:int?}")]
    private sealed class NumericKeyProbe : ComponentBase
    {
        [Parameter] public int? Key { get; set; }
    }

    [Route("/modal-compat/{Key:guid?}")]
    private sealed class GuidKeyProbe : ComponentBase
    {
        [Parameter] public Guid? Key { get; set; }
    }

    private sealed class UnroutedDialogProbe : ComponentBase
    {
        [Parameter] public int Revision { get; set; }
    }
}
