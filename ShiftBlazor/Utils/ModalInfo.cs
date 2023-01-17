using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ModalInfo
    {
        public string Name { get; set; } = default!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Key { get; set; }

        public Dictionary<string, string> Parameters { get; set; } = default!;
    }
}
