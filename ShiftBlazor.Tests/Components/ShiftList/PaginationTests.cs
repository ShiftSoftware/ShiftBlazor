using MudBlazor;
using Syncfusion.Blazor.Navigations;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList
{
    public class PaginationTests : ShiftBlazorTestContext
    {
        [Fact]
        public void ShouldHaveShiftListCascadingParameter()
        {
            var cut = RenderComponent<ShiftList<Sample>>(parameters => parameters
                .Add(p => p.Action, "/Product")
            );

            var pagination = cut.FindComponent<Pagination<Sample>>();

            Assert.NotNull(pagination.Instance.ShiftList);
        }
    }
}
