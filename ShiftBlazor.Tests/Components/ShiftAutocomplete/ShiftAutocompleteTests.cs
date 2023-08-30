using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftAutocomplete;

public class ShiftAutocompleteTests : ShiftBlazorTestContext
{
    private readonly string EntitytSet = "Product";

    [Fact]
    public void ShouldInheritMudAutocomplete()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet)
            .Add(p => p.DataValueField, "ID").Add(p => p.DataTextField, "Name")
        );

        Assert.IsAssignableFrom<MudAutocomplete<ShiftEntitySelectDTO>>(comp.Instance);
    }

    [Fact]
    public void ShouldThrowIfEntitySetIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>());
    }

    [Fact]
    public void ShouldRenderComponentCorrectly()
    {
        var cut = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        // Check if the html result contains the MudAutocomplete classes
        Assert.Contains("mud-select", cut.Markup);
        Assert.Contains("mud-autocomplete", cut.Markup);
    }

    [Fact]
    public void ShouldBeReadOnlyWhenInViewMode()
    {
        // Add a cascading State value to the context to emulate a form
        RenderTree.Add<CascadingValue<FormModes>>(parameters => parameters.Add(p => p.Value, FormModes.View));

        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        // Mud rerenders the element attributes only on interaction
        comp.Find("input").Click();

        comp.WaitForAssertion(() =>
        {
            Assert.True(comp.Instance.ReadOnly);
            // Search the generated html to check if has correct attributes,
            // This line might not be necessary since we already check for the component's ReadOnly property
            Assert.True(comp.Find("input.mud-select-input").HasAttribute("readonly"));
        });
    }

    [Fact]
    public void ShouldNotBeReadOnlyWhenInEditMode()
    {
        RenderTree.Add<CascadingValue<FormModes>>(parameters => parameters.Add(p => p.Value, FormModes.Edit));

        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        comp.Find("input").Click();

        comp.WaitForAssertion(() =>
        {
            Assert.False(comp.Instance.ReadOnly);
            Assert.False(comp.Find("input.mud-select-input").HasAttribute("readonly"));
        });
    }

    [Fact]
    public void ShouldNotBeReadOnlyWhenInCreateMode()
    {
        RenderTree.Add<CascadingValue<FormModes>>(parameters => parameters.Add(p => p.Value, FormModes.Create));

        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));
        comp.Find("input").Click();

        comp.WaitForAssertion(() =>
        {
            Assert.False(comp.Instance.ReadOnly);
            Assert.False(comp.Find("input.mud-select-input").HasAttribute("readonly"));
        });
    }

    /// <summary>
    ///     If the parent's State is 'Saving' then disable the component.
    /// </summary>
    [Fact]
    public void ShouldBeDisabledWhenSaveTaskInProgress()
    {
        // Add a cascading State value to the context to emulate a form
        RenderTree.Add<CascadingValue<FormTasks>>(parameters => parameters.Add(p => p.Value, FormTasks.Save));

        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        // Make sure Mud rerenders the element by making an interaction with the element
        comp.Find("input").Input("mud");

        comp.WaitForAssertion(() =>
        {
            Assert.True(comp.Instance.Disabled);
            Assert.True(comp.Find("input.mud-select-input").HasAttribute("disabled"));
        });
    }

    /// <summary>
    ///     Make sure the input element is not disabled or is not readonly when State is null.
    /// </summary>
    [Fact]
    public void NoCascadingValues()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        comp.Find("input").Click();

        comp.WaitForAssertion(() =>
        {
            Assert.False(comp.Instance.Disabled);
            Assert.False(comp.Instance.ReadOnly);
            Assert.False(comp.Find("input.mud-select-input").HasAttribute("disabled"));
            Assert.False(comp.Find("input.mud-select-input").HasAttribute("readonly"));
        });
    }

    /// <summary>
    ///     Checks component for correct default values.
    /// </summary>
    [Fact]
    public void DefaultValues()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        Assert.True(comp.Instance.OnlyValidateIfDirty);
        Assert.True(comp.Instance.ResetValueOnEmptyText);
        Assert.Equal(Variant.Text, comp.Instance.Variant);
    }

    /// <summary>
    ///     Make sure component's default values can be changed.
    /// </summary>
    [Fact]
    public void OverrideDefaultValues()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters => parameters
            .Add(p => p.EntitySet, EntitytSet)
            .Add(p => p.OnlyValidateIfDirty, false)
            .Add(p => p.ResetValueOnEmptyText, false)
            .Add(p => p.Variant, Variant.Filled)
        );

        Assert.False(comp.Instance.OnlyValidateIfDirty);
        Assert.False(comp.Instance.ResetValueOnEmptyText);
        Assert.Equal(Variant.Filled, comp.Instance.Variant);
    }

    /// <summary>
    ///     Test whether QueryBuilder object is created.
    /// </summary>
    [Fact]
    public void QueryBuilderTest()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, ShiftEntityDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet));

        Assert.NotNull(comp.Instance.QueryBuilder);
    }

    /// <summary>
    /// Test whether OData search url is generated correctly.
    /// </summary>
    //[Fact]
    //public void SearchCorrectResultTest()
    //{
    //    var entityName = EntitytSet;

    //    var comp = RenderComponent<ShiftAutocomplete<ShiftEntityDTO>>(parameters => parameters
    //        .Add(p => p.EntitySet, EntitytSet)
    //        .Add(p => p.SearchFunc, e => "text")
    //    );

    //    Assert.NotNull(comp.Instance.SearchFunc);
    //    comp.WaitForAssertion(() =>
    //    {
    //        Assert.Equal($"{BaseUrl}{ODataBaseUrl}/{entityName}", comp.Instance.GetODataUrl(""));
    //        Assert.Equal($"{BaseUrl}{ODataBaseUrl}/{entityName}?$top=100&$filter=contains(tolower(Name),'Sample')", comp.Instance.GetODataUrl("Sample"));
    //    });
    //}

    /// <summary>
    ///     Test whether OData http call and parse works.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ShouldReturnCorrectODataItems()
    {
        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, SampleDTO>>(parameters =>
            parameters.Add(p => p.EntitySet, EntitytSet).Add(p => p.DataValueField, "ID").Add(p => p.DataTextField, "Name")
        );

        var items = await comp.Instance.GetODataResult($"{BaseUrl}{ODataBaseUrl}/{EntitytSet}", default);

        comp.WaitForAssertion(() =>
        {
            Assert.IsType<List<ShiftEntitySelectDTO>>(items);
            Assert.Equal(Values.Count, items.Count);
        });
    }

    /// <summary>
    ///     Test whether filtered field name can be changed.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ShouldFilterCorrectColumnWhenSpecified()
    {
        var id = "ab49Q";

        var comp = RenderComponent<ShiftAutocomplete<ShiftEntitySelectDTO, SampleDTO>>(parameters => parameters
            .Add(p => p.EntitySet, EntitytSet)
            .Add(p => p.Where, q => x => x.ID == q)
            .Add(p => p.DataValueField, "ID")
            .Add(p => p.DataTextField, "Name")
        );

        var items = await comp.Instance.GetODataResult($"{BaseUrl}{ODataBaseUrl}/{EntitytSet}", default);
        comp.WaitForAssertion(() =>
        {

            Assert.Equal($"{BaseUrl}{ODataBaseUrl}/{EntitytSet}?$filter=ID eq '{id}'&$top=100",
                comp.Instance.GetODataUrl(id.ToString()));
        });
    }
}