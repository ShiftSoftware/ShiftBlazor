using ShiftSoftware.ShiftBlazor.Extensions;

namespace ShiftSoftware.ShiftBlazor.Services;

public class AppConfiguration
{
    public string BaseAddress { get; set; } = string.Empty;
    public string? UserListEndpoint { get; set; }

    private string _ApiPath = "/api";
    private string _ODataPath = "/odata";
    public string ApiPath
    {
        get => BaseAddress.AddUrlPath(_ApiPath);
        set => _ApiPath = value;
    }

    public string ODataPath
    {
        get => BaseAddress.AddUrlPath(_ODataPath);
        set => _ODataPath = value;
    }

    public Type? Index { get; set; }

    public List<LanguageInfo> Languages = new();

    public AppConfiguration AddLanguage(string culture, string label, bool rtl = false)
    {
        if (string.IsNullOrWhiteSpace(culture)) throw new NullReferenceException(nameof(culture));

        if (string.IsNullOrWhiteSpace(label))
        {
            label = culture;
        }

        Languages.Add(new LanguageInfo
        {
            CultureName = culture,
            Label = label,
            RTL = rtl
        });

        return this;
    }
}
