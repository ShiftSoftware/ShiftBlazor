using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using RichardSzalay.MockHttp;
using ShiftBlazor.Tests.Shared.DTOs;
using ShiftBlazor.Tests.Viewer.Components.ShiftList;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList;

/// <summary>
/// An ActionButton doesn't run its action on more rows than the user can write in one action
/// (GeneralActionTree.DataGridBulkWriteMaxTop). The click shows the limit instead: no request, no dialog, no callback.
/// </summary>
public class ActionButtonBulkWriteMaxTopTests : ShiftBlazorTestContext
{
    private const int MinimumAccess = 5;

    private readonly List<SelectStateDTO<User>> requests = [];

    public ActionButtonBulkWriteMaxTopTests()
    {
        MockHttp.When(HttpMethod.Post, $"{BaseUrl}/Users/bulk").Respond(async request =>
        {
            requests.Add((await request.Content!.ReadFromJsonAsync<SelectStateDTO<User>>())!);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ShiftEntityResponse<IList<User>>()),
            };
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void AtTheLimit_TheRequestIsSent(int limit)
    {
        Grant(limit);
        var cut = RenderList(Endpoint());
        Select(cut, limit);

        Click(cut);

        cut.WaitForAssertion(() => Assert.Equal(limit, Assert.Single(requests).Items.Count));
        Assert.Empty(Warnings());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void OverTheLimit_NoRequestIsSentAndTheWarningShowsBothNumbers(int limit)
    {
        Grant(limit);
        var cut = RenderList(Endpoint());
        Select(cut, limit + 1);

        Click(cut);

        var warning = Assert.Single(Warnings());
        Assert.Contains($"{limit + 1} items", warning);
        Assert.EndsWith($" {limit}.", warning);
        Assert.Empty(requests);
    }

    [Theory]
    [InlineData("en-US", "3 items are selected. The most you can change in one action is 2.")]
    [InlineData("ar-IQ", "تم تحديد 3 عنصر. الحد الأقصى الذي يمكنك تغييره في إجراء واحد هو 2.")]
    [InlineData("ku", "3 تۆمار دیاری کراون. زۆرترین ژمارە کە دەتوانیت لە یەک کرداردا بیگۆڕیت: 2.")]
    [InlineData("ru", "Выбрано элементов: 3. За одно действие можно изменить не более 2.")]
    public void OverTheLimit_TheWarningIsInTheUsersLanguage(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            Grant(2);
            var cut = RenderList(Endpoint());
            Select(cut, 3);

            Click(cut);

            Assert.Equal(expected, Assert.Single(Warnings()));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // An app can bring its own resource, or an older localization package.
    [Fact]
    public void OverTheLimit_AResourceWithoutTheText_StillGivesAReadableWarning()
    {
        Services.AddTransient(services => new ShiftBlazorLocalizer(services, typeof(ActionButtonBulkWriteMaxTopTests)));
        Grant(2);
        var cut = RenderList(Endpoint());
        Select(cut, 3);

        Click(cut);

        Assert.Equal("3 items are selected. The most you can change in one action is 2.", Assert.Single(Warnings()));
    }

    // Nothing the action does may start: whichever way the button runs, and with the confirmation on or off.
    [Theory]
    [InlineData("Endpoint", true)]
    [InlineData("Endpoint", false)]
    [InlineData("OnClick", true)]
    [InlineData("OnClick", false)]
    [InlineData("OnClickGetItems", true)]
    [InlineData("OnClickGetItems", false)]
    [InlineData("ComponentType", true)]
    [InlineData("ComponentType", false)]
    public void OverTheLimit_EveryKindOfActionIsStopped(string kind, bool disableConfirm)
    {
        Grant(2);
        var ran = 0;
        var askedForDialog = 0;
        var cut = RenderList(new()
        {
            [kind] = Action(kind, () => ran++),
            ["DisableConfirm"] = disableConfirm,
            ["OnDefaultDialogOpen"] = (SelectState<User> _) => { askedForDialog++; return ValueTask.FromResult(true); },
        });
        Select(cut, 3);

        Click(cut);

        Assert.Single(Warnings());
        Assert.Equal(0, ran);
        Assert.Equal(0, askedForDialog);
        Assert.Empty(requests);
        Assert.Empty(cut.FindComponents<PopupMessage>());
        Assert.Empty(cut.FindComponents<SelectionDialog>());
    }

    // The same actions run below the limit, so the test above stops something that works.
    [Theory]
    [InlineData("Endpoint")]
    [InlineData("OnClick")]
    [InlineData("OnClickGetItems")]
    [InlineData("ComponentType")]
    public void AtTheLimit_EveryKindOfActionRuns(string kind)
    {
        Grant(3);
        var ran = 0;
        var cut = RenderList(new()
        {
            [kind] = Action(kind, () => ran++),
            ["DisableConfirm"] = true,
        });
        Select(cut, 3);

        Click(cut);

        cut.WaitForAssertion(() => Assert.Equal(1, ran + requests.Count + cut.FindComponents<SelectionDialog>().Count));
        Assert.Empty(Warnings());
    }

    [Fact]
    public void AtTheLimit_TheConfirmationDialogIsShown()
    {
        Grant(3);
        var cut = RenderList(new() { ["Endpoint"] = "Users/bulk" });
        Select(cut, 3);

        Click(cut);

        cut.WaitForAssertion(() => Assert.Single(cut.FindComponents<PopupMessage>()));
        Assert.Empty(Warnings());
    }

    [Fact]
    public void DisableBulkWriteMaxTop_RunsTheActionOnAnyNumberOfRows()
    {
        Grant(2);
        var cut = RenderList(Endpoint(disableBulkWriteMaxTop: true));
        Select(cut, 20);

        Click(cut);

        cut.WaitForAssertion(() => Assert.Equal(20, Assert.Single(requests).Items.Count));
        Assert.Empty(Warnings());
    }

    [Fact]
    public void AllRowsSelected_CountsTheRowsOfTheWholeList()
    {
        Grant(49);
        var cut = RenderList(Endpoint());
        SelectAll(cut);

        Click(cut);

        Assert.Contains("50 items", Assert.Single(Warnings()));
        Assert.Empty(requests);
    }

    [Fact]
    public void AllRowsSelected_AtTheLimit_TheRequestIsSent()
    {
        Grant(50);
        var cut = RenderList(Endpoint());
        SelectAll(cut);

        Click(cut);

        cut.WaitForAssertion(() => Assert.True(Assert.Single(requests).All));
        Assert.Empty(Warnings());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithoutTheGrant_TheMinimumIsTheLimit(bool treeRegistered)
    {
        // The test context registers TypeAuth without a tree or a grant.
        if (treeRegistered)
            Use(new TypeAuthContextBuilder().AddActionTree<GeneralActionTree>().Build());

        var cut = RenderList(Endpoint());

        Select(cut, MinimumAccess + 1);
        Click(cut);
        Assert.Single(Warnings());
        Assert.Empty(requests);

        Deselect(cut, 1);
        Click(cut);
        cut.WaitForAssertion(() => Assert.Equal(MinimumAccess, Assert.Single(requests).Items.Count));
    }

    [Fact]
    public void WithoutTypeAuth_ThereIsNoLimit()
    {
        Services.RemoveAll<ITypeAuthService>();
        var cut = RenderList(Endpoint());
        Select(cut, 20);

        Click(cut);

        cut.WaitForAssertion(() => Assert.Equal(20, Assert.Single(requests).Items.Count));
        Assert.Empty(Warnings());
    }

    [Fact]
    public void ALimitOf0_StopsOneRow()
    {
        Grant(0);
        var cut = RenderList(Endpoint());
        Select(cut, 1);

        Click(cut);

        Assert.Single(Warnings());
        Assert.Empty(requests);
    }

    // A stopped click leaves the button as it was: no loading icon, and the next click works.
    [Fact]
    public void AfterAStoppedClick_TheButtonStillWorksAndHasNoLoadingIcon()
    {
        Grant(2);
        var cut = RenderList(Endpoint());

        Select(cut, 3);
        Click(cut);
        Assert.Empty(requests);
        Assert.Empty(Button(cut).FindComponents<MudProgressCircular>());
        Assert.Contains("Run Action", Button(cut).Markup);

        Deselect(cut, 1);
        Click(cut);
        cut.WaitForAssertion(() => Assert.Equal(2, Assert.Single(requests).Items.Count));
    }

    private static Dictionary<string, object> Endpoint(bool disableBulkWriteMaxTop = false) => new()
    {
        ["Endpoint"] = "Users/bulk",
        ["DisableConfirm"] = true,
        ["DisableBulkWriteMaxTop"] = disableBulkWriteMaxTop,
    };

    private object Action(string kind, Action run) => kind switch
    {
        "Endpoint" => "Users/bulk",
        "OnClick" => EventCallback.Factory.Create<MouseEventArgs>(this, run),
        "OnClickGetItems" => (SelectState<User> _) => { run(); return ValueTask.FromResult(true); },
        "ComponentType" => typeof(SelectionDialog),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private void Grant(int limit)
        => Use(new TypeAuthContextBuilder()
            .AddActionTree<GeneralActionTree>()
            .AddAccessTree($$"""{ "GeneralActionTree": { "DataGridBulkWriteMaxTop": {{limit}} } }""")
            .Build());

    private void Use(ITypeAuthService typeAuth)
    {
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton(typeAuth);
    }

    private IRenderedComponent<IncludeMudProviders> RenderList(Dictionary<string, object> buttonParameters)
    {
        var users = Enumerable.Range(1, 50).Select(x => new User { ID = x.ToString(), Name = $"User {x}", Email = $"user{x}@example.com" }).ToList();

        var cut = Render<IncludeMudProviders>(host => host.AddChildContent<ShiftListTestActionButton>(list =>
        {
            list.Add(x => x.Users, users);
            foreach (var parameter in buttonParameters)
                list.AddUnmatched(parameter.Key, parameter.Value);
        }));

        cut.WaitForAssertion(() => Assert.True(Button(cut).Instance.Disabled));

        return cut;
    }

    private static IRenderedComponent<ActionButton<User>> Button(IRenderedComponent<IncludeMudProviders> cut)
        => cut.FindComponent<ActionButton<User>>();

    private static ShiftList<User> List(IRenderedComponent<IncludeMudProviders> cut)
        => cut.FindComponent<ShiftList<User>>().Instance;

    private static void Select(IRenderedComponent<IncludeMudProviders> cut, int count)
        => Change(cut, list =>
        {
            foreach (var user in list.Values!.Take(count))
                list.SelectState.Toggle(user);
        });

    private static void Deselect(IRenderedComponent<IncludeMudProviders> cut, int count)
        => Change(cut, list =>
        {
            foreach (var user in list.SelectState.Items.Take(count).ToList())
                list.SelectState.Toggle(user);
        });

    private static void SelectAll(IRenderedComponent<IncludeMudProviders> cut)
        => Change(cut, list => list.SelectState.All = true);

    private static void Change(IRenderedComponent<IncludeMudProviders> cut, Action<ShiftList<User>> change)
    {
        change(List(cut));
        cut.FindComponent<ShiftListTestActionButton>().Render();
        cut.WaitForAssertion(() => Assert.False(Button(cut).Instance.Disabled));
    }

    private static void Click(IRenderedComponent<IncludeMudProviders> cut)
        => cut.Find("button.action-button").Click();

    private List<string> Warnings()
        => Services.GetRequiredService<ISnackbar>().ShownSnackbars
            .Where(x => x.Severity == Severity.Warning)
            .Select(x => x.Message!)
            .ToList();

    // A dialog component gets the selection in this parameter.
    public class SelectionDialog : ComponentBase
    {
        [Parameter]
        public SelectState<User>? Selected { get; set; }
    }
}
