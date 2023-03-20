namespace ShiftSoftware.ShiftBlazor.Tests;

public class ODataResult<T>
{
    public List<T> value { get; set; } = new();
}