using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.RevisionViewer
{
    public class RevisionViewerTests : ShiftBlazorTestContext
    {
        [Fact]
        public void ShouldRenderComponentCorrectly()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>();

            comp.FindComponent<ShiftList<RevisionDTO>>();
        }
    }
}
