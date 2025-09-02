using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Interfaces;

public interface IShortcutComponent : IDisposable
{
    public Guid Id { get; }
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; }
    public ValueTask HandleShortcut(KeyboardKeys actions);

    public static OrderedDictionary<Guid, IShortcutComponent> Components { get; set; } = new();

    public static bool Register(IShortcutComponent component)
    {
        return Components.TryAdd(component.Id, component);
    }

    public static bool Remove(Guid id)
    {
        return Components.Remove(id);
    }

    public static string CleanKeyName(string keyName)
    {
        return keyName.Replace("Key", "");
    }

    public static async Task SendKeys(IEnumerable<KeyboardKeys> keys)
    {
        if (Components.Count != 0)
        {
            await Components.Last().Value.HandleShortcut(keys.First());
        }
    }
}
