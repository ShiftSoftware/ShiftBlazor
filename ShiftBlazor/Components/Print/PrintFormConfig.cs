using MudBlazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components.Print;

public class PrintFormConfig
{
    public required List<PrintLabelValue> Types { get; set; }
    public string? DefaultTypeKey { get; set; }

    public string Icon { get; set; } = Icons.Material.Filled.Print;
    public Color Color { get; set; } = Color.Warning;
    public string Title { get; set; } = "Print Options";
    public string? ConfirmText { get; set; }
    public string? CancelText { get; set; }

    public List<FormFieldConfig> Fields { get; set; } = [];
}

public class PrintLabelValue
{
    public PrintLabelValue(string key, string? displayName)
    {
        Key = key;
        DisplayName = displayName;
    }

    public PrintLabelValue() { }

    public required string Key { get; set; }
    public string? DisplayName { get; set; }

    public override string ToString()
    {
        return DisplayName  ?? Key;
    }
}

public class FormFieldConfig
{
    public required string Key { set; get; }
    public string? DisplayName { get; set; }
    public List<string>? ApplicableTypeKeys { get; set; }
    public PrintInputType Input { get; set; } = PrintInputType.Dropdown;
    public List<PrintLabelValue> Choices { get; set; } = [];
    public string? DefaultChoiceKey { get; set; }
}

public enum PrintInputType
{
    Dropdown,
    Radio,
    Chips,
}