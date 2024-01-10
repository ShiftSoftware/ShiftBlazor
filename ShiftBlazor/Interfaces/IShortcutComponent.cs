using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Interfaces
{
    public interface IShortcutComponent : IDisposable
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Dictionary<KeyboardKeys, object> Shortcuts { get; set; }
        public ValueTask HandleShortcut(KeyboardKeys actions);

        public static Dictionary<Guid, IShortcutComponent> Components { get; set; } = new();

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
            Console.WriteLine(JsonSerializer.Serialize(Components.Values.Select(x => x.Title)));

            if (Components.Any())
            {
                Console.WriteLine(Components.Last().Value.Title);
                await Components.Last().Value.HandleShortcut(keys.First());
            }
        }


    }
}
