namespace ShiftSoftware.ShiftBlazor.Utils;

/// <summary>
/// Spells a whole number in English words: 1 → "one", 21 → "twenty-one", 578 → "five hundred seventy-eight".
/// Used for type-to-confirm prompts, where typing the count as words makes the user read it.
/// </summary>
public static class NumberToWords
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Tens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety",
    ];

    private static readonly (long Value, string Name)[] Scales =
    [
        (1_000_000_000_000, "trillion"),
        (1_000_000_000, "billion"),
        (1_000_000, "million"),
        (1_000, "thousand"),
    ];

    public static string ToEnglish(long number)
    {
        if (number == 0)
            return Ones[0];

        if (number < 0)
            return "minus " + ToEnglish(-number);

        var parts = new List<string>();

        foreach (var (value, name) in Scales)
        {
            if (number >= value)
            {
                parts.Add($"{BelowThousand((int)(number / value))} {name}");
                number %= value;
            }
        }

        if (number > 0)
            parts.Add(BelowThousand((int)number));

        return string.Join(' ', parts);
    }

    private static string BelowThousand(int number)
    {
        var parts = new List<string>();

        if (number >= 100)
        {
            parts.Add($"{Ones[number / 100]} hundred");
            number %= 100;
        }

        if (number >= 20)
        {
            parts.Add(number % 10 == 0 ? Tens[number / 10] : $"{Tens[number / 10]}-{Ones[number % 10]}");
        }
        else if (number > 0)
        {
            parts.Add(Ones[number]);
        }

        return string.Join(' ', parts);
    }
}
