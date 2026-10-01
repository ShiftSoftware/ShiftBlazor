using FluentValidation;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Pii;

public class ShiftPiiFieldTests : ShiftBlazorTestContext
{
    public ShiftPiiFieldTests()
    {
        Services.AddShiftEntityPii();
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder()
            .AddAccessTree("{\"PiiActionTree\":{\"Reveal\":[\"m\"]}}")
            .AddActionTree<PiiActionTree>()
            .Build());
        MockHttp.When(HttpMethod.Post, BaseUrl + "/api/contacts/7/pii/Phone/reveal")
            .RespondJson(new PiiRevealDTO { Value = "synthetic-phone-0088" });
    }

    [Fact]
    public void Existing_value_stays_masked_until_one_way_reveal_then_edits_or_clears_normally()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var field = new PiiFieldDTO { Display = "•••• 0088", Write = "keep" };
        var initialField = field;
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Label, "Phone")
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, field)
            .Add(p => p.FieldChanged, changed => field = changed!));

        Assert.Equal("•••• 0088", cut.Find("input").GetAttribute("value"));
        Assert.NotNull(cut.Find("input").GetAttribute("readonly"));
        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Equal("synthetic-phone-0088", cut.Find("input").GetAttribute("value")));
        Assert.Empty(cut.FindAll("button"));
        Assert.Null(cut.Find("input").GetAttribute("readonly"));
        Assert.Equal("keep", field.Write);
        Assert.Null(field.Value);

        cut.Find("input").Input("synthetic-phone-0099");
        Assert.Equal("replace", field.Write);
        Assert.Equal("synthetic-phone-0099", field.Value);

        // A form render may briefly pass the last protected wrapper back while
        // the two-way binding catches up. That must not hide or undo the edit.
        cut.Render(parameters => parameters.Add(p => p.Field, initialField));
        Assert.Equal("synthetic-phone-0099", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("readonly"));

        cut.Render(parameters => parameters.Add(p => p.Field, field));
        Assert.Equal("synthetic-phone-0099", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("");
        Assert.Equal("replace", field.Write);
        Assert.Null(field.Value);

        cut.Find("input").Input("synthetic-phone-0088");
        Assert.Equal("keep", field.Write);
        Assert.Null(field.Value);
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Create_mode_is_an_ordinary_input_without_reveal_controls()
    {
        PiiFieldDTO? field = null;
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Label, "Phone")
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, (string?)null)
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, field)
            .Add(p => p.FieldChanged, changed => field = changed));

        Assert.Empty(cut.FindAll("button"));
        Assert.Null(cut.Find("input").GetAttribute("readonly"));
        cut.Find("input").Input("synthetic-phone-0088");
        Assert.Equal("replace", field?.Write);
        Assert.Equal("synthetic-phone-0088", field?.Value);

        cut.Find("input").Input("");
        Assert.Equal("keep", field?.Write);
        Assert.Null(field?.Value);
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Missing_pii_grant_shows_only_the_masked_read_only_input()
    {
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder()
            .AddAccessTree("{}")
            .AddActionTree<PiiActionTree>()
            .Build());

        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "••••" }));

        Assert.Equal("••••", cut.Find("input").GetAttribute("value"));
        Assert.NotNull(cut.Find("input").GetAttribute("readonly"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Fresh_save_response_restores_the_mask_and_reveal_control()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal("synthetic-phone-0088", cut.Find("input").GetAttribute("value")));

        cut.Render(parameters => parameters.Add(p => p.Field,
            new PiiFieldDTO { Display = "•••• 0099", Write = "keep" }));

        Assert.Equal("•••• 0099", cut.Find("input").GetAttribute("value"));
        Assert.NotNull(cut.Find("input").GetAttribute("readonly"));
        Assert.Single(cut.FindAll("button"));
    }

    [Fact]
    public async Task Pending_reveal_keeps_a_loading_adornment_until_the_response_arrives()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        MockHttp.Expect(HttpMethod.Post, BaseUrl + "/api/contacts/7/pii/Phone/reveal")
            .Respond(() => pending.Task);
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));

        var revealTask = cut.Find("button").ClickAsync();
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindComponent<MudProgressCircular>());
            Assert.NotNull(cut.Find("[aria-label='Revealing protected value']"));
            Assert.Empty(cut.FindAll("button"));
            Assert.Contains("mud-input-root-adorned-end", cut.Find("input").ClassList);
            Assert.Equal("•••• 0088", cut.Find("input").GetAttribute("value"));
        });

        pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PiiRevealDTO { Value = "synthetic-phone-0088" })
        });
        await revealTask;

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindComponents<MudProgressCircular>());
            Assert.Equal("synthetic-phone-0088", cut.Find("input").GetAttribute("value"));
            Assert.Empty(cut.FindAll("button"));
        });
    }

    [Fact]
    public async Task Late_reveal_response_does_not_expose_the_previous_record()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        MockHttp.Expect(HttpMethod.Post, BaseUrl + "/api/contacts/7/pii/Phone/reveal")
            .Respond(() => pending.Task);
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));

        var revealTask = cut.Find("button").ClickAsync();
        cut.WaitForAssertion(() => Assert.Single(cut.FindComponents<MudProgressCircular>()));

        cut.Render(parameters => parameters
            .Add(p => p.RecordKey, "8")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0011", Write = "keep" }));
        pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PiiRevealDTO { Value = "synthetic-phone-0088" })
        });
        await revealTask;

        Assert.Equal("•••• 0011", cut.Find("input").GetAttribute("value"));
        Assert.NotNull(cut.Find("input").GetAttribute("readonly"));
        Assert.Single(cut.FindAll("button"));
    }

    [Fact]
    public void Editing_in_a_form_survives_field_rerenders_until_the_form_loads_a_new_value()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var form = new TestPiiForm();
        RenderTree.Add<CascadingValue<IShiftForm>>(parameters => parameters
            .Add(p => p.Name, "ShiftForm")
            .Add(p => p.Value, form));
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal("synthetic-phone-0088", cut.Find("input").GetAttribute("value")));
        cut.Find("input").Input("synthetic-phone-0099");

        cut.Render(parameters => parameters.Add(p => p.Field,
            new PiiFieldDTO { Display = "•••• 0088", Value = "synthetic-phone-0099", Write = "replace" }));
        Assert.Equal("synthetic-phone-0099", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("readonly"));
        Assert.Empty(cut.FindAll("button"));

        cut.Find("input").Input("synthetic-phone-00991");
        Assert.Equal("synthetic-phone-00991", cut.Find("input").GetAttribute("value"));
        Assert.Null(cut.Find("input").GetAttribute("readonly"));

        form.EditContext = new EditContext(new object());
        cut.Render(parameters => parameters.Add(p => p.Field,
            new PiiFieldDTO { Display = "•••• 0099", Write = "keep" }));
        Assert.Equal("•••• 0099", cut.Find("input").GetAttribute("value"));
        Assert.NotNull(cut.Find("input").GetAttribute("readonly"));
        Assert.Single(cut.FindAll("button"));
    }

    [Fact]
    public void Editing_a_pii_field_marks_the_form_modified()
    {
        Services.GetRequiredService<SettingManager>().Configuration.BaseAddress = BaseUrl + "/api/";
        var model = new TestPiiModel { Phone = new PiiFieldDTO { Display = "•••• 0088", Write = "keep" } };
        var form = new TestPiiForm { EditContext = new EditContext(model) };
        RenderTree.Add<CascadingValue<IShiftForm>>(parameters => parameters
            .Add(p => p.Name, "ShiftForm")
            .Add(p => p.Value, form));
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, model.Phone)
            .Add(p => p.FieldChanged, changed => model.Phone = changed)
            .Add(p => p.For, () => model.Phone));

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal("synthetic-phone-0088", cut.Find("input").GetAttribute("value")));
        Assert.False(form.EditContext.IsModified());

        cut.Find("input").Input("synthetic-phone-0099");

        Assert.True(form.EditContext.IsModified(FieldIdentifier.Create(() => model.Phone)));
        Assert.Equal("synthetic-phone-0099", model.Phone?.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reveal_uses_the_form_base_url_or_external_address(bool useExternalAddressKey)
    {
        var settings = Services.GetRequiredService<SettingManager>();
        settings.Configuration.BaseAddress = BaseUrl + "/api/";
        settings.Configuration.ExternalAddresses["contacts"] = BaseUrl + "/external-api/";
        var form = new TestPiiForm
        {
            BaseUrl = useExternalAddressKey ? null : BaseUrl + "/external-api/",
            BaseUrlKey = useExternalAddressKey ? "contacts" : null
        };
        RenderTree.Add<CascadingValue<IShiftForm>>(parameters => parameters
            .Add(p => p.Name, "ShiftForm")
            .Add(p => p.Value, form));
        MockHttp.Expect(HttpMethod.Post, BaseUrl + "/external-api/contacts/7/pii/Phone/reveal")
            .RespondJson(new PiiRevealDTO { Value = "external-value" });
        var cut = Render<ShiftPiiField>(parameters => parameters
            .Add(p => p.Endpoint, "contacts")
            .Add(p => p.RecordKey, "7")
            .Add(p => p.Member, "Phone")
            .Add(p => p.Field, new PiiFieldDTO { Display = "•••• 0088", Write = "keep" }));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Equal("external-value", cut.Find("input").GetAttribute("value")));
        MockHttp.VerifyNoOutstandingExpectation();
    }

    private sealed class TestPiiModel
    {
        public PiiFieldDTO? Phone { get; set; }
    }

    private sealed class TestPiiForm : IShiftForm, IRequest
    {
        public string? Endpoint { get; set; } = "contacts";
        public string? BaseUrl { get; set; }
        public string? BaseUrlKey { get; set; }
        public Guid Id { get; } = Guid.NewGuid();
        public string? Title { get; set; }
        public FormModes Mode { get; set; } = FormModes.Edit;
        public FormTasks TaskInProgress { get; set; }
        public string IconSvg { get; set; } = "";
        public string? NavColor { get; set; }
        public bool NavIconFlatColor { get; set; }
        public EditContext EditContext { get; set; } = new(new object());
        public IValidator? Validator => null;
        public bool AddSection(FormSection section) => throw new NotImplementedException();
        public bool RemoveSection(FormSection section) => throw new NotImplementedException();
        public List<FormSection> GetSections() => throw new NotImplementedException();
        public bool Validate() => throw new NotImplementedException();
        public bool Validate(List<FieldIdentifier> fields) => throw new NotImplementedException();
        public void DisplayError(string field, string message) => throw new NotImplementedException();
        public void DisplayError(FieldIdentifier field, string message) => throw new NotImplementedException();
        public void MarkAsChanged() => throw new NotImplementedException();
    }
}
