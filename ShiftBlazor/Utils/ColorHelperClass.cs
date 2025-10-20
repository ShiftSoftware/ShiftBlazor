// https://stackoverflow.com/a/9733420

namespace ShiftSoftware.ShiftBlazor.Utils;

public class ColorHelperClass
{
    const double RED = 0.2126;
    const double GREEN = 0.7152;
    const double BLUE = 0.0722;
    const double GAMMA = 2.4;

    private static double Luminance(double r, double g, double b)
    {
        double[] a = new double[] { r, g, b };
        for (int i = 0; i < a.Length; i++)
        {
            a[i] /= 255;
            a[i] = a[i] <= 0.03928 ? a[i] / 12.92 : Math.Pow((a[i] + 0.055) / 1.055, GAMMA);
        }
        return a[0] * RED + a[1] * GREEN + a[2] * BLUE;
    }

    private static double Contrast(int[] rgb1, int[] rgb2)
    {
        double lum1 = Luminance(rgb1[0], rgb1[1], rgb1[2]);
        double lum2 = Luminance(rgb2[0], rgb2[1], rgb2[2]);
        double brightest = Math.Max(lum1, lum2);
        double darkest = Math.Min(lum1, lum2);
        return (brightest + 0.05) / (darkest + 0.05);
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
