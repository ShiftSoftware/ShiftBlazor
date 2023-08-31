using Microsoft.AspNetCore.Components.Forms;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.FileUploader
{
    public class FileUploaderTests : ShiftBlazorTestContext
    {
        [Fact]
        public void ShouldRenderComponentCorrectly()
        {
            var comp = RenderComponent<ShiftBlazor.Components.FileUploader>();

            comp.FindComponent<InputFile>();
        }
    }
}
