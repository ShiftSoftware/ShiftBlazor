using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Interfaces;

public interface IShortcutComponent : IDisposable
{
    public Guid Id { get; }
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; }
    public ValueTask HandleShortcut(KeyboardKeys actions);

    private static OrderedDictionary<Guid, IShortcutComponent> Components { get; set; } = new();

    // The registry is static, so every component in the process shares it — and components are
    // registered from render callbacks, which do not all run on the same thread. Without this,
    // two overlapping renders corrupt the dictionary, and every later Register throws
    // "non-concurrent collections must have exclusive access" for the life of the process.
    private static object Sync { get; } = new();

    public static bool Register(IShortcutComponent component)
    {
        lock (Sync)
        {
            return Components.TryAdd(component.Id, component);
        }
    }

    public static bool Remove(Guid id)
    {
        lock (Sync)
        {
            return Components.Remove(id);
        }
    }

    public static string CleanKeyName(string keyName)
    {
        return keyName.Replace("Key", "");
    }

    public static async Task SendKeys(IEnumerable<KeyboardKeys> keys)
    {
        // Pick the target under the lock, then hand off outside it — the handler is the
        // component's own async work and must not run while the registry is held.
        IShortcutComponent? top;

        lock (Sync)
        {
            top = Components.Count == 0 ? null : Components.Last().Value;
        }

        if (top != null)
        {
            await top.HandleShortcut(keys.First());
        }
    }

    public static IShortcutComponent GetComponent(Index index)
    {
        lock (Sync)
        {
            return Components.ElementAtOrDefault(index).Value;
        }
    }
}
