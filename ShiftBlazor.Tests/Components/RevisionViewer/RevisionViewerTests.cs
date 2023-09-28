using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.RevisionViewer
{
    public class RevisionViewerTests : ShiftBlazorTestContext
    {
        [Fact]
        public void ShouldRenderComponentCorrectly()
        {
            var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>(parameters => parameters.Add(p => p.EntitySet, "Users"));

            comp.FindComponent<ShiftList<RevisionDTO>>();
        }

        //[Fact]
        //public void ShouldRenderColumnsCorrectly()
        //{
        //    var comp = RenderComponent<ShiftBlazor.Components.RevisionViewer>();

        //    var shiftList = comp.FindComponent<ShiftList<RevisionDTO>>();
        //    var list = comp.FindComponent<SfGrid<RevisionDTO>>();
            
        //    var excluded = new List<string>();
        //    excluded.AddRange(shiftList.Instance.DefaultExcludedColumns);
        //    // // No need to add this to excluded list as it will be added again
        //    //excluded.AddRange(shiftList.Instance.ExcludedColumns);

        //    var fields = list.Instance.Columns.Where(x => x.Visible);
        //    var properties = typeof(RevisionDTO).GetProperties().Where(x => !excluded.Contains(x.Name)).Select(x => x.Name);

        //    Assert.Contains(fields, x => x.Field == nameof(RevisionDTO.SavedByUserID) && x.Field != x.HeaderText);
        //    Assert.Contains(properties, x => fields.Select(o => o.Field).Contains(x));
        //}
    }
}
