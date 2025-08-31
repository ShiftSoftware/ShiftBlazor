using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftEntity.Model.Dtos;


namespace ShiftSoftware.ShiftBlazor.Components;

public class ListChildContext<T> where T : ShiftEntityDTOBase, new()
{
    public ShiftList<T> Self { get; }
    public RenderFragment? DraftColumn { get; }
    public RenderFragment? BrandForeignColumn { get; }
    public bool IsEmbed { get; }
    public Uri? CurrentUri { get; }
    public SelectState<T> SelectState { get; }
    public Guid Id => Self.Id;
    public Action Reload;
    public Func<T, Task> SelectRow;

    public ListChildContext(ShiftList<T> self)
    {
        Self = self;
        DraftColumn = self.GetDraftColumn();
        BrandForeignColumn = self.GetBrandForeignColumn();
        IsEmbed = self.IsEmbed;
        CurrentUri = self.CurrentUri;
        SelectState = self.SelectState;
        Reload = self.Reload;
        SelectRow = self.SelectRow;
    }
}