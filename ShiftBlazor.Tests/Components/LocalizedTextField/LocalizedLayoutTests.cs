using System.ComponentModel.DataAnnotations;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.LocalizedTextField;

public class LocalizedLayoutTests : ShiftBlazorTestContext
{
    private static List<LanguageInfo> FiveLanguages() =>
    [
        new() { CultureName = "en-US", Label = "English" },
        new() { CultureName = "ar-IQ", Label = "Arabic", RTL = true },
        new() { CultureName = "ku-IQ", Label = "Kurdish", RTL = true },
        new() { CultureName = "fr-FR", Label = "French" },
        new() { CultureName = "tr-TR", Label = "Turkish" },
    ];

    [Theory]
    [InlineData(LanguageLayout.Grid)]
    [InlineData(LanguageLayout.Row)]
    [InlineData(LanguageLayout.Stacked)]
    [InlineData(LanguageLayout.Dropdown)]
    [InlineData(LanguageLayout.Tabs)]
    [InlineData(LanguageLayout.Expandable)]
    public void Every_layout_keeps_an_input_per_language_and_saves_them_as_json(LanguageLayout layout)
    {
        var cut = Render<LocalizedLayoutForm>(p => p
            .Add(x => x.LanguageLayout, layout)
            .Add(x => x.Languages, FiveLanguages()));

        var inputs = cut.FindAll("input");
        Assert.Equal(5, inputs.Count);

        inputs[0].Change("Hello");
        inputs[4].Change("Merhaba");

        Assert.Equal("{\"en\":\"Hello\",\"ar\":\"\",\"ku\":\"\",\"fr\":\"\",\"tr\":\"Merhaba\"}", cut.Instance.Model.Name);
    }

    [Fact]
    public void Languages_added_and_removed_on_the_fly_keep_the_text_already_typed()
    {
        var cut = Render<LocalizedLayoutForm>(p => p.Add(x => x.Languages, FiveLanguages().Take(2).ToList()));
        cut.FindAll("input")[0].Change("Hello");

        cut.Render(p => p.Add(x => x.Languages, FiveLanguages()));
        Assert.Equal(5, cut.FindAll("input").Count);
        Assert.Equal("Hello", cut.FindAll("input")[0].GetAttribute("value"));

        cut.Render(p => p.Add(x => x.Languages, FiveLanguages().Skip(3).ToList()));
        Assert.Equal(2, cut.FindAll("input").Count);

        cut.Render(p => p.Add(x => x.Languages, FiveLanguages()));
        Assert.Equal("Hello", cut.FindAll("input")[0].GetAttribute("value"));
    }

    [Fact]
    public void Dropdown_switches_to_the_first_invalid_language_on_submit()
    {
        var cut = Render<LocalizedLayoutForm>(p => p
            .Add(x => x.LanguageLayout, LanguageLayout.Dropdown)
            .Add(x => x.Languages, FiveLanguages()));
        cut.FindAll("input")[0].Change("Hello");

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("AR", cut.FindComponent<LocalizedLanguageSwitch>().Instance.Selected.ToUpperInvariant()));
        Assert.Equal(0, cut.Instance.ValidSubmits);
    }

    [Fact]
    public void Expandable_opens_the_other_languages_when_one_of_them_is_invalid()
    {
        var cut = Render<LocalizedLayoutForm>(p => p
            .Add(x => x.LanguageLayout, LanguageLayout.Expandable)
            .Add(x => x.Languages, FiveLanguages()));
        cut.FindAll("input")[0].Change("Hello");
        Assert.Contains("display: none;", cut.Markup);

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.DoesNotContain("display: none;", cut.Markup));
    }

    [Fact]
    public void A_replaced_field_no_longer_blocks_the_save()
    {
        var twoLanguages = FiveLanguages().Take(2).ToList();
        var cut = Render<LocalizedLayoutForm>(p => p.Add(x => x.Languages, twoLanguages));
        cut.FindAll("input")[0].Change("Hello");

        cut.Render(p => p.Add(x => x.FieldKey, 1));
        cut.FindAll("input")[1].Change("Marhaba");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.ValidSubmits));
    }
}

public class LocalizedModel
{
    [Required(ErrorMessage = "Name is required.")]
    public string? Name { get; set; }
}
