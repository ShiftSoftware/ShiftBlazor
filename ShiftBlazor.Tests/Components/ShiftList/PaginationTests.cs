﻿
namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftList
{
    public class PaginationTests : ShiftBlazorTestContext
    {
        [Fact]
        public void ShouldHaveShiftListCascadingParameter()
        {
            var cut = RenderComponent<ShiftList<SampleDTO>>(parameters => parameters
                .Add(p => p.Action, "/Product")
            );

            var pagination = cut.FindComponent<Pagination<SampleDTO>>();

            Assert.NotNull(pagination.Instance.ShiftList);
        }
    }
}