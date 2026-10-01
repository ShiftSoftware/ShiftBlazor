using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace ShiftSoftware.ShiftBlazor.Tests.Extensions;

public class EditContextExtensionTests
{
    [Fact]
    public void A_nested_field_keeps_its_messages_on_its_own_object()
    {
        var model = new Order { Lines = [new OrderLine()] };
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var lineLabel = FieldIdentifier.Create(() => model.Lines[0].Label);

        // The order has a member with the same name. The line's message must not land there.
        Assert.False(editContext.ValidateDataAnnotation([lineLabel], store));
        Assert.Equal(["Line label is required."], editContext.GetValidationMessages(lineLabel));
        Assert.Empty(editContext.GetValidationMessages(FieldIdentifier.Create(() => model.Label)));

        model.Lines[0].Label = "line";
        Assert.True(editContext.ValidateDataAnnotation([lineLabel], store));
        Assert.Empty(editContext.GetValidationMessages());
    }

    private class Order
    {
        public string? Label { get; set; } = "order";
        public List<OrderLine> Lines { get; set; } = [];
    }

    private class OrderLine
    {
        [Required(ErrorMessage = "Line label is required.")]
        public string? Label { get; set; }
    }
}
