using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ODataFilter
    {
        public Guid? Id { get; set; }
        public string? Field { get; set; }
        public ODataOperator Operator { get; set; }
        public object? Value { get; set; }
    }
}
