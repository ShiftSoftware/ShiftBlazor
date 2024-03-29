﻿using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftForm;

public class ShiftEntityFormTests : ShiftBlazorTestContext
{
    public readonly string path = "Product";

    [Fact]
    public void ShouldInheritShiftFormBasic()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        Assert.IsAssignableFrom<ShiftFormBasic<SampleDTO>>(comp.Instance);
    }

    [Fact]
    public void ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RenderComponent<ShiftEntityForm<SampleDTO>>());
    }

    [Fact]
    public void ShouldOpenInCreateMode()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        Assert.Equal(FormModes.Create, comp.Instance.Mode);
    }

    [Fact]
    public void ShouldOpenInViewMode()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
        );

        Assert.Equal(FormModes.View, comp.Instance.Mode);
    }

    //[Fact]
    //public void ShouldAllowSettingMode()
    //{
    //    var comp = RenderComponent<ShiftEntityForm<Sample>>(parameters => parameters
    //        .Add(p => p.Action, path)
    //        .Add(p => p.Mode, Form.Modes.Archive)
    //    );

    //    Assert.Equal(Form.Modes.Archive, comp.Instance.Mode);
    //}

    //[Fact]
    public void ShouldRenderHeaderToolbarButtonsCorrectly()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
        );

        var buttons = comp.FindComponent<MudToolBar>().FindComponents<MudButton>();

        Assert.All(buttons, x => x.Instance.Disabled.Equals(false));
    }

    [Fact]
    public void ShouldDisableHeaderToolbarButtons()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        var buttons = comp.FindComponent<MudToolBar>().FindComponents<MudButton>();

        Assert.All(buttons, x => x.Instance.Disabled.Equals(true));
    }

    //[Fact]
    public void ShouldDisableDeleteButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
            .Add(p => p.DisableDelete, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .First(x => x.Instance.Text == "Delete")
            .FindComponent<MudButton>()
            .Instance;

        Assert.True(button.Disabled);
    }

    [Fact]
    public void ShouldNotRenderDeleteButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.HideDelete, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .FirstOrDefault(x => x.Instance.Text == "Delete");

        Assert.Null(button);
    }

    //[Fact]
    public void ShouldDisableEditButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
            .Add(p => p.DisableEdit, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .First(x => x.Instance.Text == "Edit")
            .FindComponent<MudButton>()
            .Instance;

        Assert.True(button.Disabled);
    }

    [Fact]
    public void ShouldNotRenderEditButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.HideEdit, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .FirstOrDefault(x => x.Instance.Text == "Edit");

        Assert.Null(button);
    }

    //[Fact]
    public void ShouldDisableRevisionsButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
            .Add(p => p.DisableRevisions, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .First(x => x.Instance.Text == "View Revisions")
            .FindComponent<MudButton>()
            .Instance;

        Assert.True(button.Disabled);
    }

    [Fact]
    public void ShouldNotRenderRevisionsButton()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.HideRevisions, true)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .FirstOrDefault(x => x.Instance.Text == "View Revisions");

        Assert.Null(button);
    }

    //[Fact]
    public void ShouldInvokePrintFunction()
    {
        var invoked = false;
        var taskStarted = false;
        var taskFinished = false;

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Key, "1")
            .Add(p => p.OnTaskStart, (task) => taskStarted = task.Data == FormTasks.Print)
            .Add(p => p.OnTaskFinished, (task) => taskFinished = task == FormTasks.Print)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .FirstOrDefault(x => x.Instance.Text == "Print")?
            .FindComponent<MudButton>();

        Assert.NotNull(button);
        Assert.False(button.Instance.Disabled);

        button.Find("button").Click();

        comp.WaitForAssertion(() =>
        {
            Assert.True(taskStarted, "Print task failed to start");
            Assert.True(invoked);
            Assert.True(taskFinished, "Print task failed to finish");
        });
    }

    [Fact]
    public void ShouldAddFullPathToItemUrl()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        var url = BaseUrl.AddUrlPath(ApiBaseUrl, path);

        Assert.Equal(url, comp.Instance.ItemUrl);
    }

    [Fact]
    public void ShouldAddFullPathToItemUrlWithItemKey()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
        );

        var url = BaseUrl.AddUrlPath(ApiBaseUrl, path, "1");

        Assert.Equal(url, comp.Instance.ItemUrl);
    }

    //[Fact]
    //public void ShouldHideSubmitButton()
    //{
    //    var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
    //        .Add(p => p.Key, "1")
    //        .Add(p => p.Action, path)
    //    );

    //    Assert.True(comp.Instance.HideSubmit);
    //}

    [Fact]
    public void ShouldHaveCreateAsSubmitButtonText()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        Assert.Equal("Create", comp.Instance._SubmitText);
    }

    //[Fact]
    public void ShouldHaveSaveAsSubmitButtonText()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
        );

        var button = comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .First(x => x.Instance.Text == "Edit")
            .Find("button");

        button.Click();

        Assert.Equal("Save", comp.Instance._SubmitText);
    }

    [Fact]
    public void ShouldFetchItem()
    {
        var taskInprogress = FormTasks.None;

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
            .Add(p => p.OnTaskStart, (task) => taskInprogress = task.Data)
        );

        comp.WaitForAssertion(() =>
        {
            Assert.Equal(FormTasks.Fetch, taskInprogress);
            Assert.Equivalent(Values.First(), comp.Instance.Value);
        });
    }

    [Fact]
    public async Task ShouldKeepACopyOfOriginalValue()
    {
        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
        );

        comp.WaitForAssertion(() => Assert.NotNull(comp.Instance.OriginalValue));

        var item = JsonSerializer.Deserialize<SampleDTO>(comp.Instance.OriginalValue);
        Assert.Equivalent(comp.Instance.Value, item);

        comp.Instance.Value.Name = "Sample Name Changed";
        await comp.Instance.RestoreOriginalValue();

        Assert.Equivalent(comp.Instance.Value, item);


    }

    //[Fact]
    //public void ShouldDeleteItemAfterConfirming()
    //{
    //    var deleteTaskStarted = false;
    //    var deleteTaskFinished = false;

    //    var comp = RenderComponent<IncludeMudProviders>(_params => _params.AddChildContent<ShiftEntityForm<SampleDTO>>(
    //        parameters => parameters
    //            .Add(p => p.Key, "1")
    //            .Add(p => p.Action, path)
    //            .Add(p => p.OnTaskStart, (task) => deleteTaskStarted = task.Data == FormTasks.Delete)
    //            .Add(p => p.OnTaskFinished, (task) => deleteTaskFinished = task == FormTasks.Delete)
    //    ));

    //    var entityForm = comp.FindComponent<ShiftEntityForm<SampleDTO>>();
    //    Assert.False(entityForm.Instance.Value.IsDeleted, "Item should not be deleted");

    //    var deleteButton = comp.FindComponent<MudToolBar>()
    //        .FindComponents<MudTooltip>()
    //        .First(x => x.Instance.Text == "Delete");

    //    deleteButton.InvokeAsync(() => deleteButton.Find("button").Click());

    //    comp.WaitForAssertion(() => Assert.True(deleteTaskStarted, "deleteTaskStarted failed"));


    //    var msgBox = comp.FindComponent<PopupMessage>();

    //    Assert.Equal("Delete", msgBox.Instance.ConfirmText);


    //    comp.WaitForAssertion(() => Assert.False(deleteTaskFinished, "delete task finished before confirming"));

    //    msgBox.InvokeAsync(() => msgBox.FindAll("button").First(x => x.TextContent.Contains("Delete")).Click());

    //    comp.WaitForAssertion(() => Assert.True(deleteTaskFinished, "delete task did not finish after confirm"));

    //    Assert.True(entityForm.Instance.Value.IsDeleted, "Item should be deleted");
    //}

    //[Fact]
    //public void ShouldChangeToEditMode()
    //{
    //    var comp = RenderComponent<ShiftEntityForm<Sample>>(parameters => parameters
    //        .Add(p => p.Key, "1")
    //        .Add(p => p.Action, path)
    //    );

    //    Assert.NotEqual(Form.Modes.Edit, comp.Instance.Mode);

    //    comp.FindComponent<MudToolBar>()
    //        .FindComponents<MudTooltip>()
    //        .First(x => x.Instance.Text == "Edit")
    //        .Find("button")
    //        .Click();

    //    Assert.Equal(Form.Modes.Edit, comp.Instance.Mode);
    //}

    //[Fact]
    public async Task ShouldRevertChanges()
    {
        var value = Values.First();

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
        );

        comp.FindComponent<MudToolBar>()
            .FindComponents<MudTooltip>()
            .First(x => x.Instance.Text == "Edit")
            .Find("button")
            .Click();
        
        comp.Instance.Value.Name = "Sample 00";

        await comp.Instance.CancelChanges();

        // This is not supposed to work because EditContext.IsModified should return true
        // and the function should hang until user clicks on confirm button
        // but instead it returns false and processed to execute the rest of the function
        Assert.Equal(value.Name, comp.Instance.Value.Name);
    }

    [Fact]
    public async Task ShouldSetTitle()
    {
        var value = new SampleDTO
        {
            Name = "Sample 1",
            LastName = "Last"
        };

        var title = "this is a title";

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.Value, value)
            .Add(p => p.Title, title)
        );

        Assert.EndsWith(title, comp.Instance.DocumentTitle);
        Assert.StartsWith("Creating new", comp.Instance.DocumentTitle);

        await comp.Instance.ValidSubmitHandler(comp.Instance.EditContext);

        Assert.StartsWith("Viewing", comp.Instance.DocumentTitle);
        Assert.EndsWith($"{title} ({comp.Instance.Value.ID})", comp.Instance.DocumentTitle);

        await comp.Instance.EditItem();

        Assert.StartsWith("Editing", comp.Instance.DocumentTitle);
        Assert.EndsWith($"{title} ({comp.Instance.Value.ID})", comp.Instance.DocumentTitle);
    }

    //[Fact]
    //public void ShouldReziseForm()
    //{
    //    var comp = RenderComponent<MudDialogInstance>(DialogParameters => DialogParameters
    //        .Add(p => p.Id, Guid.NewGuid())
    //        .Add(p => p.Title, "Dialog Title")
    //        .Add(p => p.Options, new DialogOptions { FullScreen = false })
    //        .Add<ShiftEntityForm<SampleDTO>>(p => p.Content, z => z
    //            .Add(p => p.Action, path)
    //            .Add(p => p.Key, "1")
    //        )
    //    );

    //    var entityForm = comp.FindComponent<ShiftEntityForm<SampleDTO>>().Instance;

    //    Assert.False(entityForm.MudDialog!.Options.FullScreen);

    //    var maxButton = comp
    //        .FindComponent<MudToolBar>()
    //        .FindComponents<MudTooltip>()
    //        .First(x => x.Instance.Text == "Maximize");

    //    comp.InvokeAsync(() => maxButton.Find("button").Click());

    //    comp.WaitForAssertion(() => Assert.True(entityForm.MudDialog!.Options.FullScreen));
    //}


    [Fact]
    public async Task ShouldValidSubmitHandler()
    {
        var value = new SampleDTO
        {
            Name = "Test",
            LastName = "Test",
        };
        var taskStarted = false;
        var taskFinished = false;
        var submitHandled = false;

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
            .Add(p => p.OnTaskStart, (task) => taskStarted = task.Data == FormTasks.Save)
            .Add(p => p.OnTaskFinished, (task) => taskFinished = task == FormTasks.Save)
            .Add(p => p.OnValidSubmit, () => submitHandled = true)
            .Add(p => p.Value, value)
        );

        await comp.Instance.SubmitHandler(comp.Instance.EditContext);

        Assert.True(taskStarted, "Task didn't start");
        Assert.True(taskFinished, "Task didn't finish");
        Assert.True(submitHandled, "Task didn't submit");
    }

    //[Fact]
    //public async Task ShouldParseEntityResponse()
    //{

    //}

    [Fact]
    public async Task ShouldCopyValueOnSetValue()
    {
        var value = new SampleDTO
        {
            Name = "Test",
            LastName = "Test",
        };

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Action, path)
        );

        Assert.Equal(JsonSerializer.Serialize(comp.Instance.Value), comp.Instance.OriginalValue);

        await comp.Instance.SetValue(value, false);

        await Task.Delay(10);

        Assert.NotEqual(JsonSerializer.Serialize(comp.Instance.Value), comp.Instance.OriginalValue);

        await comp.Instance.SetValue(value);

        await Task.Delay(10);

        Assert.Equal(JsonSerializer.Serialize(comp.Instance.Value), comp.Instance.OriginalValue);
    }

    //[Fact]
    //public async Task Should()
    //{
    //    var value = new Sample
    //    {
    //        Name = "Test"
    //    };

    //    var comp = RenderComponent<ShiftEntityForm<Sample>>(parameters => parameters
    //        .Add(p => p.Action, path)
    //    );

    //    comp.Instance.Value.Name = "Test2";

    //    //await comp.Instance.SetValue(value);

    //    Assert.False(comp.Instance.editContext.IsModified());
    //}

    [Fact]
    public async Task ShouldUpdateUrl()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var url = navManager.Uri;

        var comp = RenderComponent<ShiftEntityForm<SampleDTO>>(parameters => parameters
            .Add(p => p.Key, "1")
            .Add(p => p.Action, path)
        );

        await comp.Instance.UpdateUrl(1);

        Assert.NotEqual(url, navManager.Uri);
    }

    //[Fact]
    //public async Task ShouldUpdateUrl2()
    //{
    //    var navManager = Services.GetRequiredService<NavigationManager>();
    //    var url = navManager.Uri;
    //    //navManager.NavigateTo($"/?modal={{\"{path}\"}}");

    //    RenderTree.Add<CascadingValue<MudDialogInstance>>(parameters =>
    //        parameters.Add(p => p.Value, new MudDialogInstance())
    //    );

    //    var comp = RenderComponent<ShiftEntityForm<Sample>>(parameters => parameters
    //        .Add(p => p.Key, "1")
    //        .Add(p => p.Action, path)
    //    );

    //    await comp.Instance.UpdateUrl(1);

    //    Assert.NotEqual(url, navManager.Uri);
    //}
}