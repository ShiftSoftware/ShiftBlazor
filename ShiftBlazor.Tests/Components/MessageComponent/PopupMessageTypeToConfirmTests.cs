using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.MessageComponent;

/// <summary>
/// With TypeToConfirm set, PopupMessage shows an instruction and a text field under the message, and keeps
/// its confirm button disabled until the user types the expected text. Neither the button, Ctrl+S nor
/// Enter can confirm before that.
/// </summary>
public class PopupMessageTypeToConfirmTests : ShiftBlazorTestContext
{
    private async Task<(IRenderedComponent<IncludeMudProviders> Host, IDialogReference Dialog)> ShowAsync(string? typeToConfirm)
    {
        var host = Render<IncludeMudProviders>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters
        {
            { "Message", new Message("Continue?", "Are you sure you want to perform this action on 3 items?") },
            { "ConfirmText", "Yes" },
            { "CancelText", "Close" },
            { "TypeToConfirm", typeToConfirm },
        };

        var dialog = await host.InvokeAsync(() => dialogService.ShowAsync<PopupMessage>("", parameters));

        return (host, dialog);
    }

    private static IRenderedComponent<MudButtonExtended> ConfirmButton(IRenderedComponent<IncludeMudProviders> host) =>
        host.FindComponents<MudButtonExtended>().First(x => x.Instance.KeyboardKey == KeyboardKeys.KeyS);

    [Fact]
    public async Task WithoutTypeToConfirm_ShowsNoFieldAndConfirmIsEnabled()
    {
        var (host, _) = await ShowAsync(null);

        Assert.Empty(host.FindAll("input"));
        Assert.False(ConfirmButton(host).Instance.Disabled);
    }

    [Fact]
    public async Task ShowsTheInstructionAndAFieldWithoutALabel()
    {
        var (host, _) = await ShowAsync("three");

        Assert.Contains("Type \"three\" below to confirm", host.Markup.Replace("<b>", "").Replace("</b>", ""));
        Assert.Empty(host.FindAll(".mud-input-label"));
    }

    [Fact]
    public async Task ConfirmIsDisabledUntilTheTextIsTyped()
    {
        var (host, dialog) = await ShowAsync("three");

        Assert.True(ConfirmButton(host).Instance.Disabled);

        host.Find("input").Input("3");
        Assert.True(ConfirmButton(host).Instance.Disabled);

        host.Find("input").Input(" Three ");
        Assert.False(ConfirmButton(host).Instance.Disabled);

        ConfirmButton(host).Find("button").Click();
        var result = await dialog.Result;
        Assert.False(result!.Canceled);
    }

    [Theory]
    [InlineData("twenty-one")]
    [InlineData("twenty one")]
    [InlineData("Twenty  One")]
    public async Task HyphensSpacesAndCaseAreIgnored(string typed)
    {
        var (host, _) = await ShowAsync("twenty-one");

        host.Find("input").Input(typed);

        Assert.False(ConfirmButton(host).Instance.Disabled);
    }

    [Fact]
    public async Task CtrlSAndEnterDoNotConfirmBeforeTheTextIsTyped()
    {
        var (host, dialog) = await ShowAsync("three");
        var popup = host.FindComponent<PopupMessage>();

        await host.InvokeAsync(() => popup.Instance.HandleShortcut(KeyboardKeys.KeyS).AsTask());
        host.Find("input").KeyDown("Enter");

        Assert.False(dialog.Result.IsCompleted);
        Assert.Single(host.FindComponents<PopupMessage>());
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "one")]
    [InlineData(13, "thirteen")]
    [InlineData(40, "forty")]
    [InlineData(21, "twenty-one")]
    [InlineData(100, "one hundred")]
    [InlineData(578, "five hundred seventy-eight")]
    [InlineData(1_005, "one thousand five")]
    [InlineData(12_345, "twelve thousand three hundred forty-five")]
    [InlineData(2_000_000, "two million")]
    public void SpellsCountsInEnglish(long number, string expected)
    {
        Assert.Equal(expected, NumberToWords.ToEnglish(number));
    }
}
