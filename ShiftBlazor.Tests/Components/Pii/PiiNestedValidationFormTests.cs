using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using FluentValidation;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.Pii;

public class PiiNestedValidationFormTests : ShiftBlazorTestContext
{
    public PiiNestedValidationFormTests()
    {
        Services.AddShiftEntityPii();
        Services.RemoveAll<ITypeAuthService>();
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder()
            .AddAccessTree("{\"PiiActionTree\":{\"Reveal\":[\"m\"]}}")
            .AddActionTree<PiiActionTree>().Build());
    }

    [Fact]
    public void Nested_annotation_and_fluent_errors_bind_to_the_child_and_clear_after_correction()
    {
        var cut = Render<PiiNestedValidationForm>(p => p.Add(x => x.Validator, new CustomerValidator()));
        var form = cut.FindComponent<ShiftEntityForm<NestedPiiCustomerDTO>>();
        // Like any field of a nested object, the phone is checked by its section and when it changes.
        // Validation of the whole form checks the model's own members.
        Assert.False(cut.FindComponent<FormSection>().Instance.Validate());
        Assert.Contains("Additional phone is required.", cut.Markup);
        var phone = cut.FindComponent<ShiftPiiField>();
        phone.Find("input").Input("synthetic-too-long");
        Assert.Contains("Additional phone is too long.", phone.Markup);
        phone.Find("input").Input("blocked");
        Assert.Contains("Use a different phone.", phone.Markup);
        phone.Find("input").Input("valid");
        Assert.True(form.Instance.Validate());
        Assert.Empty(form.Instance.EditContext.GetValidationMessages());
        Assert.DoesNotContain("Use a different phone.", cut.Markup);
    }

    [Fact]
    public async Task Existing_nested_phone_reveals_by_id_and_saves_in_the_parent_form()
    {
        var saved = Saved();
        MockHttp.When(HttpMethod.Get, BaseUrl + "/nested-contacts/7").RespondJson(new ShiftEntityResponse<NestedPiiCustomerDTO>(saved));
        MockHttp.When(HttpMethod.Post, BaseUrl + "/nested-contacts/7/pii/" + Uri.EscapeDataString("Phones[22].Number") + "/reveal")
            .RespondJson(new PiiRevealDTO { Value = "stored" });
        var requests = 0;
        MockHttp.When(HttpMethod.Put, BaseUrl + "/nested-contacts/7").Respond(request =>
        {
            var json = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var dto = JsonSerializer.Deserialize<NestedPiiCustomerDTO>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal("22", dto.Phones[0].ID);
            Assert.Equal("edited", dto.Phones[0].Number!.Value);
            Assert.Equal("replace", dto.Phones[0].Number!.Write);
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(JsonSerializer.Serialize(new ShiftEntityResponse<NestedPiiCustomerDTO>(saved))) };
        });
        var cut = Render<PiiNestedValidationForm>(p => p.Add(x => x.Key, "7"));
        var form = cut.FindComponent<ShiftEntityForm<NestedPiiCustomerDTO>>();
        cut.WaitForAssertion(() => Assert.Equal("•••• 2222", cut.FindComponent<ShiftPiiField>().Find("input").GetAttribute("value")));
        Assert.True(form.Instance.Validate());
        await cut.InvokeAsync(() => form.Instance.EditItem());
        var phone = cut.FindComponent<ShiftPiiField>();
        phone.Find("button").Click();
        phone.WaitForAssertion(() => Assert.Equal("stored", phone.Find("input").GetAttribute("value")));
        phone.Find("input").Input("");
        Assert.Contains("Additional phone is required.", phone.Markup);
        Assert.False(cut.FindComponent<FormSection>().Instance.Validate());
        phone.Find("input").Input("edited");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Equal(1, requests));
        cut.WaitForAssertion(() => Assert.Equal("•••• 2222", cut.FindComponent<ShiftPiiField>().Find("input").GetAttribute("value")));
    }

    [Fact]
    public void Nested_server_errors_appear_on_the_correct_phone_and_retry_clears_them()
    {
        var attempts = 0;
        MockHttp.When(HttpMethod.Post, BaseUrl + "/nested-contacts").Respond(_ =>
        {
            attempts++;
            var response = attempts == 1 ? new ShiftEntityResponse<NestedPiiCustomerDTO>
            {
                Message = new Message { Title = "Model Validation Error", SubMessages = [
                    new Message { For = "Phones[0].Number", SubMessages = [new Message { Title = "Server nested phone rule." }] }
                ] }
            } : new ShiftEntityResponse<NestedPiiCustomerDTO>(Saved());
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.Created)
            { Content = new StringContent(JsonSerializer.Serialize(response)) };
        });
        MockHttp.When(HttpMethod.Get, BaseUrl + "/nested-contacts/7").RespondJson(new ShiftEntityResponse<NestedPiiCustomerDTO>(Saved()));
        var cut = Render<PiiNestedValidationForm>();
        cut.FindComponent<ShiftPiiField>().Find("input").Input("valid");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("Server nested phone rule.", cut.FindComponent<ShiftPiiField>().Markup));
        cut.FindComponent<ShiftPiiField>().Find("input").Input("edited");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Equal(2, attempts));
        cut.WaitForAssertion(() => Assert.DoesNotContain("Server nested phone rule.", cut.Markup));
    }

    private static NestedPiiCustomerDTO Saved() => new()
    { ID = "7", Phones = [new() { ID = "22", Number = new() { Display = "•••• 2222", Write = "keep" } }] };
    private class CustomerValidator : AbstractValidator<NestedPiiCustomerDTO>
    {
        public CustomerValidator() => RuleForEach(x => x.Phones).ChildRules(phone =>
            phone.RuleFor(x => x.Number!.Value).NotEqual("blocked").WithMessage("Use a different phone.")
                .When(x => x.Number?.Write == "replace"));
    }
}

public class NestedPiiCustomerDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public List<NestedPiiPhoneDTO> Phones { get; set; } = [];
}
public class NestedPiiPhoneDTO : ShiftEntityDTOBase
{
    public override string? ID { get; set; }
    [Pii(PiiKind.Phone), Required(ErrorMessage = "Additional phone is required."),
     StringLength(10, ErrorMessage = "Additional phone is too long.")]
    public PiiFieldDTO? Number { get; set; }
}
