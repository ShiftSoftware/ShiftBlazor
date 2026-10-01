using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using FluentValidation;
using RichardSzalay.MockHttp;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Pii;

public class PiiValidationFormTests : ShiftBlazorTestContext
{
    public PiiValidationFormTests()
    {
        Services.AddShiftEntityPii();
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder()
            .AddAccessTree("{\"PiiActionTree\":{\"Reveal\":[\"m\"]}}")
            .AddActionTree<PiiActionTree>().Build());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_submit_shows_required_errors_and_correction_allows_a_masked_save(bool submitOnly)
    {
        var requests = 0;
        MockHttp.When(HttpMethod.Post, BaseUrl + "/validation-contacts").Respond(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(new ShiftEntityResponse<ValidatedPiiContactDTO>(Saved())))
            };
        });
        MockHttp.When(HttpMethod.Get, BaseUrl + "/validation-contacts/7").RespondJson(new ShiftEntityResponse<ValidatedPiiContactDTO>(Saved()));
        var cut = Render<PiiValidationForm>(p => p.Add(x => x.OnlyValidateOnSubmit, submitOnly));
        var form = cut.FindComponent<ShiftEntityForm<ValidatedPiiContactDTO>>();

        cut.Find("form").Submit();
        Assert.Contains("Phone is required.", cut.Markup);
        Assert.Equal(0, requests);
        var phone = cut.FindComponents<ShiftPiiField>().First();
        phone.Find("input").Input(" ");
        cut.Find("form").Submit();
        Assert.Contains("Phone is required.", cut.Markup);
        Assert.Equal(0, requests);

        phone.Find("input").Input("new-phone");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Equal(1, requests));
        cut.WaitForAssertion(() => Assert.DoesNotContain("Phone is required.", cut.Markup));
        cut.WaitForAssertion(() => Assert.Equal("•••• 0088", cut.FindComponents<ShiftPiiField>().First().Find("input").GetAttribute("value")));
    }

    [Fact]
    public void String_and_email_annotations_validate_raw_values_in_a_section()
    {
        var cut = Render<PiiValidationForm>();
        var fields = cut.FindComponents<ShiftPiiField>();
        fields[0].Find("input").Input("synthetic-too-long");
        fields[1].Find("input").Input("invalid-email");
        Assert.False(cut.FindComponent<FormSection>().Instance.Validate());
        Assert.Contains("Phone is too long.", cut.Markup);
        Assert.Contains("Enter a valid email.", cut.Markup);
        fields[0].Find("input").Input("valid");
        fields[1].Find("input").Input("a@example.test");
        Assert.True(cut.FindComponent<FormSection>().Instance.Validate());
        Assert.DoesNotContain("Phone is too long.", cut.Markup);
        Assert.DoesNotContain("Enter a valid email.", cut.Markup);
    }

    [Fact]
    public void Server_field_errors_remain_inline_and_retry_clears_them()
    {
        var attempts = 0;
        MockHttp.When(HttpMethod.Post, BaseUrl + "/validation-contacts").Respond(_ =>
        {
            attempts++;
            var response = attempts == 1
                ? new ShiftEntityResponse<ValidatedPiiContactDTO>
                {
                    Message = new Message { Title = "Model Validation Error", SubMessages = [
                        new Message { For = "Phone", Title = "Phone", SubMessages = [new Message { Title = "Server phone rule." }] }
                    ] }
                }
                : new ShiftEntityResponse<ValidatedPiiContactDTO>(Saved());
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };
        });
        MockHttp.When(HttpMethod.Get, BaseUrl + "/validation-contacts/7").RespondJson(new ShiftEntityResponse<ValidatedPiiContactDTO>(Saved()));
        var cut = Render<PiiValidationForm>();
        cut.FindComponents<ShiftPiiField>()[0].Find("input").Input("valid");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("Server phone rule.", cut.Markup));
        Assert.Equal(FormModes.Create, cut.FindComponent<ShiftEntityForm<ValidatedPiiContactDTO>>().Instance.Mode);
        cut.FindComponents<ShiftPiiField>()[0].Find("input").Input("new-phone");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Equal(2, attempts));
        cut.WaitForAssertion(() => Assert.DoesNotContain("Server phone rule.", cut.Markup));
    }

    [Fact]
    public async Task Masked_keep_passes_required_validation_and_clear_after_reveal_fails()
    {
        MockHttp.When(HttpMethod.Get, BaseUrl + "/validation-contacts/7").RespondJson(new ShiftEntityResponse<ValidatedPiiContactDTO>(Saved()));
        MockHttp.When(HttpMethod.Post, BaseUrl + "/validation-contacts/7/pii/Phone/reveal").RespondJson(new PiiRevealDTO { Value = "stored" });
        var cut = Render<PiiValidationForm>(p => p.Add(x => x.Key, "7"));
        var form = cut.FindComponent<ShiftEntityForm<ValidatedPiiContactDTO>>();
        cut.WaitForAssertion(() => Assert.Equal("•••• 0088", cut.FindComponents<ShiftPiiField>()[0].Find("input").GetAttribute("value")));
        Assert.True(form.Instance.Validate());
        await cut.InvokeAsync(() => form.Instance.EditItem());
        var phone = cut.FindComponents<ShiftPiiField>()[0];
        phone.Find("button").Click();
        phone.WaitForAssertion(() => Assert.Equal("stored", phone.Find("input").GetAttribute("value")));
        Assert.True(form.Instance.Validate());
        phone.Find("input").Input("");
        Assert.False(form.Instance.Validate());
        Assert.Contains("Phone is required.", cut.Markup);
        phone.Find("input").Input("stored");
        Assert.True(form.Instance.Validate());
        Assert.DoesNotContain("Phone is required.", cut.Markup);
    }

    [Fact]
    public void Fluent_raw_value_errors_are_displayed_and_clear_on_correction()
    {
        var cut = Render<PiiValidationForm>(p => p.Add(x => x.Validator, new ContactValidator()));
        var phone = cut.FindComponents<ShiftPiiField>()[0];
        phone.Find("input").Input("blocked");
        Assert.Contains("Use a different phone.", cut.Markup);
        var form = cut.FindComponent<ShiftEntityForm<ValidatedPiiContactDTO>>();
        Assert.Contains("Use a different phone.", form.Instance.EditContext.GetValidationMessages());
        phone.Find("input").Input("valid");
        Assert.DoesNotContain("Use a different phone.", cut.Markup);
        Assert.Empty(form.Instance.EditContext.GetValidationMessages());
    }

    [Fact]
    public void Basic_form_sections_validate_required_values_without_an_entity_id()
    {
        var cut = Render<PiiBasicValidationForm>();
        var section = cut.FindComponent<FormSection>();
        Assert.False(section.Instance.Validate());
        Assert.Contains("Phone is required.", cut.Markup);
        var phone = cut.FindComponent<ShiftPiiField>();
        phone.Find("input").Input("valid");
        Assert.True(section.Instance.Validate());
        phone.Find("input").Input("");
        Assert.False(section.Instance.Validate());
        Assert.Contains("Phone is required.", cut.Markup);
    }

    private static ValidatedPiiContactDTO Saved() => new()
    {
        ID = "7", Phone = new() { Display = "•••• 0088", Write = "keep" },
        Email = new() { Display = "a•••@example.test", Write = "keep" }
    };

    private class ContactValidator : AbstractValidator<ValidatedPiiContactDTO>
    {
        public ContactValidator() => RuleFor(x => x.Phone!.Value).NotEqual("blocked")
            .WithMessage("Use a different phone.").When(x => x.Phone?.Write == "replace");
    }
}

public class ValidatedPiiContactDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    [Pii(PiiKind.Phone), Required(ErrorMessage = "Phone is required."), StringLength(10, ErrorMessage = "Phone is too long.")]
    public PiiFieldDTO? Phone { get; set; }
    [Pii(PiiKind.Email), EmailAddress(ErrorMessage = "Enter a valid email.")]
    public PiiFieldDTO? Email { get; set; }
}

public class BasicPiiModel
{
    [Pii(PiiKind.Phone), Required(ErrorMessage = "Phone is required.")]
    public PiiFieldDTO? Phone { get; set; }
}
