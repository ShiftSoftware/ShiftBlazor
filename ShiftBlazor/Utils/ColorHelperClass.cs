namespace ShiftSoftware.ShiftBlazor.Utils;

public class ColorHelperClass
{
    // Relative luminance and contrast ratio as WCAG 2.0 defines them.
    private static double Linearize(int channel)
    {
        var value = channel / 255d;
        return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static double RelativeLuminance(int[] rgb)
        => 0.2126 * Linearize(rgb[0]) + 0.7152 * Linearize(rgb[1]) + 0.0722 * Linearize(rgb[2]);

    private static double Contrast(int[] first, int[] second)
    {
        var a = RelativeLuminance(first) + 0.05;
        var b = RelativeLuminance(second) + 0.05;
        return a > b ? a / b : b / a;
    }

    private static int[] ConvertCssRgbToIntArray(string cssRgbValue)
    {
        // Remove "rgb(" and ")" from the string and split the values by commas
        string[] rgbValues = cssRgbValue.Replace("rgb(", "").Replace(")", "").Split(',');

        // Convert the string values to integers using LINQ
        int[] rgbIntArray = rgbValues.Select(value => int.Parse(value.Trim())).ToArray();

        return rgbIntArray;
    }

    private static int[] ConvertCssHexToIntArray(string cssHexColor)
    {
        // Remove "#" from the hex color string
        string hexValue = cssHexColor.Replace("#", "");

        // Parse the hex values for red, green, and blue
        int r = int.Parse(hexValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        int g = int.Parse(hexValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        int b = int.Parse(hexValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new int[] { r, g, b };
    }

    public static double GetContrast(string color1, int[] rgb2)
    {
        if (color1.StartsWith('#'))
        {
            return Contrast(rgb2, ConvertCssHexToIntArray(color1));
        }

        return Contrast(rgb2, ConvertCssRgbToIntArray(color1));
    }

    public static string GetToolbarStyles(string? color, bool calculateContrast)
    {
        var style = "";
        var contrastMagicNumber = 4.5;

        if (string.IsNullOrWhiteSpace(color))
        {
            return style;
        }

        if (calculateContrast)
        {
            double contrast = GetContrast(color, new int[] { 255, 255, 255 });
            var _color = contrast > contrastMagicNumber ? "#fff" : "#000";
            style += $"color: {_color};";
        }

        style += $"background-color: {color};";

        return style;
    }
}
