namespace ShiftSoftware.ShiftBlazor.Enums;

public enum LanguageLayout
{
    /// <summary>One input per language in a responsive grid that wraps onto new rows.</summary>
    Grid,

    /// <summary>One input per language on a single row that scrolls sideways when the inputs don't fit.</summary>
    Row,

    /// <summary>One full-width input per language, one under the other.</summary>
    Stacked,

    /// <summary>A single input with a language dropdown beside it.</summary>
    Dropdown,

    /// <summary>A single input with a tab per language above it.</summary>
    Tabs,

    /// <summary>The first language's input, with the other languages behind a toggle.</summary>
    Expandable,
}
