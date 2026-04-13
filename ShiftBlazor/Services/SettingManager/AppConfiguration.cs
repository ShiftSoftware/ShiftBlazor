using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Services;

public class AppConfiguration
{
    public string BaseAddress { get; set; } = string.Empty;

    public Dictionary<string, string?> ExternalAddresses = [];
    public string? UserListEndpoint { get; set; }


    /// <summary>
    /// A list of thumbnail sizes to generate when a new image is uploaded.
    /// Width x Height format.
    /// </summary>
    public List<ValueTuple<int, int>> ThumbnailSizes = [(250, 250), (500, 500), (1000, 1000)];

    public List<LanguageInfo> Languages = [];
    public List<LanguageInfo> ContentLanguages = [];


    public AppConfiguration AddLanguage(string culture, string label, LanguageScope scope)
    {
        return this.AddLanguage(culture, label, false, scope);
    }

    public AppConfiguration AddLanguage(string culture, string label, bool rtl = false, LanguageScope scope = LanguageScope.Both)
    {
        //NullReferenceException.ThrowIfNull(culture, nameof(culture));
        if (string.IsNullOrWhiteSpace(culture)) throw new NullReferenceException(nameof(culture));

        if (string.IsNullOrWhiteSpace(label))
        {
            label = culture;
        }

        if (scope.HasFlag(LanguageScope.UI))
        {
            Languages.Add(new LanguageInfo
            {
                CultureName = culture,
                Label = label,
                RTL = rtl
            });
        }

        if (scope.HasFlag(LanguageScope.Content))
        {
            ContentLanguages.Add(new LanguageInfo
            {
                CultureName = culture,
                Label = label,
                RTL = rtl
            });
        }

        return this;
    }
}
