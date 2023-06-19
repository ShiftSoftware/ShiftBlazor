using AngleSharp.Css.Dom;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using System.Linq;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.LanguageSwitcher;

public class LanguageSwitcherTests: ShiftBlazorTestContext
{
    [Fact]
    public void ShouldRenderComponentCorrectly()
    {
        var comp = RenderComponent<ShiftBlazor.Components.LanguageSwitcher>();

        comp.FindComponent<MudMenu>();
    }

    [Fact]
    public void ShouldRenderMenuItemsPerLanguage()
    {
        var comp = RenderComponent<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();

        comp.FindAll("button.mud-button-root")[0].Click();
        Assert.Equal(SettingManager.Configuration.Languages.Count, comp.FindAll("div.mud-list-item").Count);
    }

    [Fact]
    public void ShouldRenderMenuItemLabelCorrectly()
    {
        var comp = RenderComponent<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();

        comp.FindAll("button.mud-button-root")[0].Click();
        Assert.All(comp.FindAll("div.mud-list-item"), (menu) =>
        {
            SettingManager.Configuration.Languages.Select(x => x.Label).Contains(menu.TextContent);
        });
    }

    [Fact]
    public void ShouldChangeSelectedLanguage()
    {
        var comp = RenderComponent<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );

        var SettingManager = Services.GetRequiredService<SettingManager>();
        var selectedLangauge = SettingManager.Settings.Language?.CultureName;

        comp.FindAll("button.mud-button-root")[0].Click();
        comp.FindAll("div.mud-list-item")[1].Click();

        Assert.NotEqual(selectedLangauge, SettingManager.Settings.Language?.CultureName);
    }

    [Fact]
    public void ShouldHaveDefaultSelectedValue()
    {
        var comp = RenderComponent<IncludeMudProviders>(parameters => parameters
            .AddChildContent<ShiftBlazor.Components.LanguageSwitcher>()
        );
        
        comp.FindAll("button.mud-button-root")[0].Click();
        var items = comp.FindAll("div.mud-list-item");
        var selected = items.Where(x =>
        {
            var css = x.GetStyle().CssText;
            return css.Contains("background-color");
        });

        Assert.Single(selected);
    }
}