using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Tests.Components;

public class ShiftFormBasicTests : ShiftBlazorTestContext
{

    [Fact]
    public void ShouldRenderComponentCorrectly()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        Assert.Equal(Form.Modes.Create, cut.Instance.Mode);
        cut.HasComponent<EditForm>();
    }

    [Fact]
    public void ShouldUseEditFormComponent()
    {
        var value = new Sample();

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.Value, value));

        cut.HasComponent<EditForm>();
    }

    [Fact]
    public void ShouldCreateAndPassEditContextToEditForm()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var editForm = cut.FindComponent<EditForm>().Instance;

        Assert.Same(editForm.EditContext, cut.Instance.editContext);
    }

    [Fact]
    public void ShouldRenderTitleCorrectly()
    {
        var title = "this is a form";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.Title, title));

        var toolbar = cut.FindComponent<MudToolBar>();

        Assert.Contains(title, toolbar.Markup);
    }

    [Fact]
    public void ShouldRenderIconCorrectly()
    {
        var icon = Icons.Material.Filled.Abc;

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.IconSvg, icon));

        var toolbar = cut.FindComponent<MudToolBar>();

        Assert.Contains(icon, toolbar.Markup);
    }

    [Fact]
    public void ShouldAddValidatorAndDisableReflection()
    {
        var validator = new SampleValidator();
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.Validator, validator));

        var fluentValidator = cut.FindComponent<Blazored.FluentValidation.FluentValidationValidator>().Instance;

        Assert.NotNull(fluentValidator.Validator);
        Assert.True(fluentValidator.DisableAssemblyScanning);
    }

    [Fact]
    public void ShouldChangeTaskOnValidSubmit()
    {
        var value = new Sample
        {
            Name = "Person",
            LastName = "Person Last Name",
        };

        var isSaving = false;
        ShiftFormBasic<Sample> form = default!;

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.OnValidSubmit, () => isSaving = form?.TaskInProgress == Form.Tasks.Save)
        );

        form = cut.Instance;

        cut.Find("footer button[type='submit']").Click();

        Assert.True(isSaving);
    }

    [Fact]
    public void ShouldRenderChildContentCorrectly()
    {
        var content = "Hello, world, how is the weather?";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.AddChildContent(content));

        var body = cut.Find(".form-body");

        Assert.Contains(content, body.InnerHtml);
    }

    [Fact]
    public void ShouldCascadeValuesToChildContent()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .AddChildContent<ShiftAutocomplete<Sample>>(_params => _params.Add(p => p.EntitySet, "Product"))
        );

        var auto = cut.FindComponent<ShiftAutocomplete<Sample>>().Instance;
        
        Assert.NotNull(auto.Mode);
        Assert.NotNull(auto.TaskInProgress);
    }

    [Fact]
    public void ShouldUseFluentValidatorWhenValidatorParameterIsNotUsed()
    {
        var value = new Sample();
        var isInvalid = false;

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.OnInvalidSubmit, () => isInvalid = true)
        );

        cut.Find("footer button[type='submit']").Click();

        Assert.True(isInvalid);
    }

    [Fact]
    public void ShouldNotRenderHeaderToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.DisableHeaderToolbar, true));

        Assert.Throws<ElementNotFoundException>(() =>
        {
            cut.Find("header .mud-toolbar");
        });
    }

    [Fact]
    public void ShouldNotRenderFooterToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.DisableFooterToolbar, true));

        Assert.Throws<ElementNotFoundException>(() =>
        {
            cut.Find("footer .mud-toolbar");
        });
    }

    [Fact]
    public void ShouldRenderToolbarTemplatesCorrectly()
    {
        Func<string, string> text = (e) => $"This is the {e} section in the header";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudChip>(p => p.ToolbarStartTemplate, z => z.AddChildContent(text("1st")))
            .Add<MudChip>(p => p.ToolbarCenterTemplate, z => z.AddChildContent(text("2nd")))
            .Add<MudChip>(p => p.ToolbarEndTemplate, z => z.AddChildContent(text("3rd")))
        );

        var chips = cut.FindComponent<MudToolBar>().FindComponents<MudChip>();

        //Make sure they are in the correct order
        Assert.Contains(text("1st"), chips.ElementAt(0).Markup);
        Assert.Contains(text("2nd"), chips.ElementAt(1).Markup);
        Assert.Contains(text("3rd"), chips.ElementAt(2).Markup);
    }

    [Fact]
    public void ShouldRenderFooterToolbarTemplatesCorrectly()
    {
        Func<string, string> text = (e) => $"This is the {e} section in the footer";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudChip>(p => p.FooterToolbarStartTemplate, z => z.AddChildContent(text("1st")))
            .Add<MudChip>(p => p.FooterToolbarCenterTemplate, z => z.AddChildContent(text("2nd")))
            .Add<MudChip>(p => p.FooterToolbarEndTemplate, z => z.AddChildContent(text("3rd")))
        );

        var chips = cut.FindComponents<MudToolBar>().Last().FindComponents<MudChip>();

        //Make sure they are in the correct order
        Assert.Contains(text("1st"), chips.ElementAt(0).Markup);
        Assert.Contains(text("2nd"), chips.ElementAt(1).Markup);
        Assert.Contains(text("3rd"), chips.ElementAt(2).Markup);
    }

    [Fact]
    public void ShouldNotRenderToolbarControlsTemplateWhenNotInAModal()
    {
        var text = $"This is the controls section";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudChip>(p => p.ToolbarControlsTemplate, z => z.AddChildContent(text))
        );

        var toolbar = cut.FindComponent<MudToolBar>();

        Assert.DoesNotContain(text, toolbar.Markup);
    }


    [Fact]
    public void ShouldRenderToolbarControlsTemplateCorrectly()
    {
        var text = $"This is the controls section";

        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters => parameters.Add(p => p.Value, new MudDialogInstance()));

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudTooltip>(p => p.ToolbarControlsTemplate, z => z.Add(p => p.Text, text))
            .Add<MudTooltip>(p => p.ToolbarEndTemplate, z => z.Add(p => p.Text, "some text"))
        );

        var tooltips = cut.FindComponent<MudToolBar>().FindComponents<MudTooltip>();

        //Make sure they are in the correct order
        Assert.Contains(text, tooltips.ElementAt(1).Instance.Text);
        Assert.Contains("Close", tooltips.ElementAt(2).Instance.Text);
    }

    [Fact]
    public void ShouldRenderOneSpacerInHeaderToolbarByDefault()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var spacers = cut.FindComponent<MudToolBar>().FindComponents<MudSpacer>();

        Assert.Single(spacers);
    }

    [Fact]
    public void ShouldRenderTwoSpacersIfToolbarCenterTemplateIsNotNull()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.ToolbarCenterTemplate, "some text")
        );

        var spacers = cut.FindComponent<MudToolBar>().FindComponents<MudSpacer>();

        Assert.Equal(2, spacers.Count);
    }

    [Fact]
    public void ShouldRenderDividerIfIsInModalAndToolbarEndIsNotNull()
    {
        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters => parameters.Add(p => p.Value, new MudDialogInstance()));
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.ToolbarEndTemplate, "some text")
        );

        cut.FindComponent<MudToolBar>().HasComponent<MudDivider>();
    }

    [Fact]
    public void ShouldRenderHeaderTemplateInHeader()
    {
        var text = "2nd header";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.HeaderTemplate, text)
        );

        Assert.Contains(text, cut.Find("header").ToMarkup());
    }

    [Fact]
    public void ShouldRenderFooterTemplateInFooter()
    {
        var text = "2nd footer";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.FooterTemplate, text)
        );

        Assert.Contains(text, cut.Find("footer").ToMarkup());
    }

    [Fact]
    public void ShouldRenderOneSpacerInFooterToolbarByDefault()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var spacers = cut.FindComponents<MudToolBar>().Last().FindComponents<MudSpacer>();

        Assert.Single(spacers);
    }

    [Fact]
    public void ShouldRenderTwoSpacersIfFooterToolbarCenterTemplateIsNotNull()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.FooterToolbarCenterTemplate, "some text")
        );

        var spacers = cut.FindComponents<MudToolBar>().Last().FindComponents<MudSpacer>();

        Assert.Equal(2, spacers.Count);
    }

    [Fact]
    public void ShouldRenderMessageWhenAlertEnabled()
    {
        var text = "should display alert message";
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        cut.Instance.ShowAlert(text, Severity.Normal, 5);
        cut.WaitForAssertion(() => Assert.Contains(text, cut.Find("header").ToMarkup()));
    }

    [Fact]
    public void ShouldMakeFormScrollableInModal()
    {
        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters => parameters.Add(p => p.Value, new MudDialogInstance()));
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        cut.Find(".shift-scrollable-content-wrapper");
    }

    [Fact]
    public void ShouldNotRenderSubmitButtonWhenDisabled()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        cut.Instance.HideSubmit = true;
        //render the component again after flag has been changed
        cut.Render();
        
        var buttons = cut.FindComponents<MudToolBar>().Last().FindComponents<MudButton>();
        //Footer toolbar should not have any buttons with the type of submit
        Assert.Empty(buttons.Where(x => x.Instance.ButtonType == ButtonType.Submit));
    }

    [Fact]
    public void ShouldChangeSubmitButtonText()
    {
        var text = "this is the button to submit.";
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.SubmitText, text)
        );

        var buttons = cut.FindComponents<MudToolBar>().Last().FindComponents<MudButton>();
        var submitButton = buttons.First(x => x.Instance.ButtonType == ButtonType.Submit);

        Assert.Contains(text, submitButton.Markup);
    }

    [Fact]
    public void ShouldHaveALoadingIconWhenSaving()
    {
        var cut = RenderComponent<ShiftFormBasic<ShiftEntityDTOBase>>(parameters => parameters
            .Add(p => p.Value, new ShiftEntityDTOBase())
            .Add(p => p.OnValidSubmit, async () => await Task.Delay(1000))
        );

        var button = cut.Find("footer button[type='submit']");
        button.Click();
        cut.WaitForAssertion(() => Assert.Contains("mud-progress-circular", button.ToMarkup()));
    }

    [Fact]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public void ShouldInvokeADelegateAndChangeTask()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var Task = Form.Tasks.None;

        _ = cut.Instance.RunTask(Form.Tasks.Custom, async () => Task = cut.Instance.TaskInProgress);

        Assert.NotEqual(Form.Tasks.None, Task);
        Assert.Equal(Form.Tasks.None, cut.Instance.TaskInProgress);
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    [Fact]
    public void ShouldCatchException()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        _ = cut.Instance.RunTask(Form.Tasks.Custom, () => throw new Exception());
        
        Assert.Equal(Form.Tasks.None, cut.Instance.TaskInProgress);
    }

    //[Fact]
    //public async Task ShouldMarkAsUnmodified()
    //{
    //    var value = new Sample();
    //    var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
    //        .Add(p => p.Value, value)
    //    );

    //    var form = cut.Instance;

    //    Assert.False(form.editContext.IsModified());

    //    form.Value.Name = "name";
    //    value.Name = "name2";

    //    Assert.True(form.editContext.IsModified());

    //    await form.SetValue(new Sample());

    //    Assert.False(form.editContext.IsModified());
    //}
}
