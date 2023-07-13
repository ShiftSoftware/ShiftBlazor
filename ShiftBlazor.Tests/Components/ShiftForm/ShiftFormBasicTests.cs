using Blazored.FluentValidation;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftForm;

public class ShiftFormBasicTests : ShiftBlazorTestContext
{
    [Fact]
    public void ShouldRenderComponentCorrectly()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        Assert.Equal(FormModes.Create, cut.Instance.Mode);
        cut.FindComponent<EditForm>();
    }

    [Fact]
    public void ShouldUseEditFormComponent()
    {
        var value = new Sample();

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.Value, value));

        cut.FindComponent<EditForm>();
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
        Assert.Equal(title, cut.Instance.DocumentTitle);
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

        var fluentValidator = cut.FindComponent<FluentValidationValidator>().Instance;

        Assert.NotNull(fluentValidator.Validator);
        Assert.True(fluentValidator.DisableAssemblyScanning);
    }

    [Fact]
    public void ShouldChangeTaskOnValidSubmit()
    {
        var value = new Sample
        {
            Name = "Person",
            LastName = "Person Last Name"
        };

        var isSaving = false;
        ShiftFormBasic<Sample> form = default!;

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.OnValidSubmit, () => isSaving = form?.TaskInProgress == FormTasks.Save)
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
            .AddChildContent<ShiftAutocomplete<ShiftEntitySelectDTO, Sample>>(_params => _params.Add(p => p.EntitySet, "Product"))
        );

        var auto = cut.FindComponent<ShiftAutocomplete<ShiftEntitySelectDTO, Sample>>().Instance;

        Assert.NotNull(auto.Mode);
        Assert.NotNull(auto.TaskInProgress);
    }

    /// <summary>
    ///     Should use FluentValidator using assembly scanning when Validator parameter is not used.
    /// </summary>
    [Fact]
    public void ShouldUseFluentValidator()
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
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters =>
            parameters.Add(p => p.DisableHeaderToolbar, true));

        Assert.Throws<ElementNotFoundException>(() => { cut.Find("header .mud-toolbar"); });
    }

    [Fact]
    public void ShouldNotRenderFooterToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters =>
            parameters.Add(p => p.DisableFooterToolbar, true));

        Assert.Throws<ElementNotFoundException>(() => { cut.Find("footer .mud-toolbar"); });
    }

    [Fact]
    public void ShouldRenderToolbarTemplatesCorrectly()
    {
        Func<string, string> text = e => $"This is the {e} section in the header";

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
        Func<string, string> text = e => $"This is the {e} section in the footer";

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
    public void ShouldNotRenderToolbarControlsAndDividerTemplate()
    {
        var text = "This is the controls section";

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudChip>(p => p.ToolbarControlsTemplate, z => z.AddChildContent(text))
        );

        var toolbar = cut.FindComponent<MudToolBar>();

        Assert.Throws<ComponentNotFoundException>(() => toolbar.FindComponent<MudDivider>());
        Assert.DoesNotContain(text, toolbar.Markup);
    }

    [Fact]
    public void ShouldRenderToolbarControlsTemplateCorrectly()
    {
        var text = "This is the controls section";

        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters =>
            parameters.Add(p => p.Value, new MudDialogInstance()));

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add<MudTooltip>(p => p.ToolbarControlsTemplate, z => z.Add(p => p.Text, text))
            .Add<MudTooltip>(p => p.ToolbarEndTemplate, z => z.Add(p => p.Text, "some text"))
        );

        var tooltips = cut.FindComponent<MudToolBar>().FindComponents<MudTooltip>();

        //Make sure they are in the correct order
        Assert.Contains(text, tooltips.ElementAt(1).Instance.Text);
        Assert.Contains("Close", tooltips.ElementAt(2).Instance.Text);
    }

    /// <summary>
    ///     Should have one spacer component in header toolbar if ToolbarCenterTemplate is null.
    /// </summary>
    [Fact]
    public void ShouldRenderOneSpacerInHeaderToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var spacers = cut.FindComponent<MudToolBar>().FindComponents<MudSpacer>();

        Assert.Single(spacers);
    }

    /// <summary>
    ///     Should add another spacer when ToolbarCenterTemplate has value to center it.
    /// </summary>
    [Fact]
    public void ShouldRenderTwoSpacersInHeaderToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.ToolbarCenterTemplate, "some text")
        );

        var spacers = cut.FindComponent<MudToolBar>().FindComponents<MudSpacer>();

        Assert.Equal(2, spacers.Count);
    }

    /// <summary>
    ///     Should render a divider element when form is open in a modal and ToolbarEndTemplate is not null.
    /// </summary>
    [Fact]
    public void ShouldRenderDivider()
    {
        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters =>
            parameters.Add(p => p.Value, new MudDialogInstance()));
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.ToolbarEndTemplate, "some text")
        );

        cut.FindComponent<MudToolBar>().FindComponent<MudDivider>();
    }

    /// <summary>
    ///     Should not render divider when ToolbarEndTemplate is null.
    /// </summary>
    [Fact]
    public void ShouldNotRenderDivider()
    {
        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters =>
            parameters.Add(p => p.Value, new MudDialogInstance()));
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        Assert.Throws<ComponentNotFoundException>(() => cut.FindComponent<MudToolBar>().FindComponent<MudDivider>());
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

    /// <summary>
    ///     Should have one spacer component in footer toolbar if FooterToolbarCenterTemplate is null
    /// </summary>
    [Fact]
    public void ShouldRenderOneSpacerInFooterToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        var spacers = cut.FindComponents<MudToolBar>().Last().FindComponents<MudSpacer>();

        Assert.Single(spacers);
    }

    /// <summary>
    ///     Should add another spacer when FooterToolbarCenterTemplate has value to center it.
    /// </summary>
    [Fact]
    public void ShouldRenderTwoSpacersInFooterToolbar()
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
        RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters =>
            parameters.Add(p => p.Value, new MudDialogInstance()));
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        cut.Find(".shift-scrollable-content-wrapper");
    }

    [Fact]
    public void ShouldNotRenderSubmitButton()
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
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters
            .Add(p => p.Value, new Sample() { Name = "First", LastName = "Last" })
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

        var Task = FormTasks.None;

        _ = cut.Instance.RunTask(FormTasks.Custom, async () => Task = cut.Instance.TaskInProgress);

        Assert.NotEqual(FormTasks.None, Task);
        Assert.Equal(FormTasks.None, cut.Instance.TaskInProgress);
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    [Fact]
    public void ShouldCatchException()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>();

        _ = cut.Instance.RunTask(FormTasks.Custom, () => throw new Exception());

        Assert.Equal(FormTasks.None, cut.Instance.TaskInProgress);
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