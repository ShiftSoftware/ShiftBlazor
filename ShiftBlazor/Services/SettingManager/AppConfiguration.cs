using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using System.Reflection;

namespace ShiftSoftware.ShiftBlazor.Services;

public class AppConfiguration
{
    public string BaseAddress { get; set; } = string.Empty;

    public Dictionary<string, string?> ExternalAddresses = new();
    public string? UserListEndpoint { get; set; }

    internal string _ApiPath = "/api";
    //private string _ODataPath = "/odata";
    public string ApiPath
    {
        get => BaseAddress.AddUrlPath(_ApiPath);
        set => _ApiPath = value;
    }

    //public string ODataPath
    //{
    //    get => BaseAddress.AddUrlPath(_ODataPath);
    //    set => _ODataPath = value;
    //}

    /// <summary>
    /// A list of thumbnail sizes to generate when a new image is uploaded.
    /// Width x Height format.
    /// </summary>
    public List<ValueTuple<int, int>> ThumbnailSizes = [(250, 250), (500, 500), (1000, 1000)];

    public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

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
