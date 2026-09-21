using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using System.Linq;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.LanguageSwitcher;

public class LanguageSwitcherTests: ShiftBlazorTestContext
{
    /// <summary>
    /// The switcher's MudMenu activates on hover (ActivationEvent = MouseOver), so the items only
    /// exist in the popover once the pointer enters the menu; clicking the activator does nothing.
    /// MudMenu ignores pointers that cannot hover (touch, pen) and opens after a short hover
    /// delay, hence the mouse pointer type and the wait.
    /// </summary>
    private static IReadOnlyList<IElement> OpenMenu(IRenderedComponent<IncludeMudProviders> comp)
    {
        comp.Find(".mud-menu").PointerEnter(new PointerEventArgs { PointerType = "mouse" });
        comp.WaitForAssertion(() => Assert.NotEmpty(comp.FindAll("[role='menuitem']")), TimeSpan.FromSeconds(3));
        return comp.FindAll("[role='menuitem']");
    }

    [Fact]
    public void ShouldRenderComponentCorrectly()
    {
        var comp = Render<ShiftBlazor.Components.LanguageSwitcher>();

        comp.FindComponent<MudMenu>();
    }

    [Fact]
    public void ShouldRenderMenuItemsPerLanguage()
    {
        var comp = Render<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();

        var items = OpenMenu(comp);
        Assert.Equal(SettingManager.Configuration.Languages.Count, items.Count);
    }

    [Fact]
    public void ShouldRenderMenuItemLabelCorrectly()
    {
        var comp = Render<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();

        var labels = SettingManager.Configuration.Languages.Select(x => x.Label).ToList();
        Assert.All(OpenMenu(comp), menu => Assert.Contains(menu.TextContent.Trim(), labels));
    }

    [Fact]
    public void ShouldChangeSelectedLanguage()
    {
        var comp = Render<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();
        var selectedLangauge = SettingManager.Settings.Language?.CultureName;

        OpenMenu(comp)[1].Click();

        comp.WaitForAssertion(() => Assert.NotEqual(selectedLangauge, SettingManager.Settings.Language?.CultureName));
    }

    [Fact]
    public void ShouldHaveDefaultSelectedValue()
    {
        var comp = Render<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );
        
        var items = OpenMenu(comp);
        var selected = items.Where(x =>
        {
            var css = x.GetStyle().CssText;
            return css.Contains("background-color");
        });

        Assert.Single(selected);
    }
}