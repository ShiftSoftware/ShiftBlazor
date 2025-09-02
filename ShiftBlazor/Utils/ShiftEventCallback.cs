
namespace ShiftSoftware.ShiftBlazor.Utils;

public class ShiftEvent<T> : ShiftEvent
{
    public T Data { get; set; }
    
    public ShiftEvent(T data)
    {
        Data = data;
    }
}

public class ShiftEvent
{
    public bool ShouldPreventDefault { get; set; }
}
