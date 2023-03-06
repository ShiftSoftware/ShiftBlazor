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
        cut.FindComponent<EditForm>();
    }

    [Fact]
    public void ShouldRenderComponentCorrectly2()
    {
        var value = new Sample();

        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.Value, value));

        cut.FindComponent<EditForm>();
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

        void SubmitHandler()
        {
            isSaving = form?.TaskInProgress == Form.Tasks.Save;
        }

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

        Assert.Throws<Bunit.ElementNotFoundException>(() =>
        {
            cut.Find("header .mud-toolbar");
        });
    }

    [Fact]
    public void ShouldNotRenderFooterToolbar()
    {
        var cut = RenderComponent<ShiftFormBasic<Sample>>(parameters => parameters.Add(p => p.DisableFooterToolbar, true));

        Assert.Throws<Bunit.ElementNotFoundException>(() =>
        {
            cut.Find("footer .mud-toolbar");
        });
    }
}
