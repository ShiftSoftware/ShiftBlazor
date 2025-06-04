using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Utils;

public class ODataFilter
{
    public Guid? Id { get; set; }
    public string? Field { get; set; }
    public ODataOperator Operator { get; set; }
    public object? Value { get; set; }
    public string? CastToType { get; set; }
}


public static class ODataPrimitiveTypes
{
    public const string String = "Edm.String";
    public const string Bool = "Edm.Boolean";
    public const string DateTime = "Edm.DateTimeOffset";
    public const string Date = "Edm.Date";
    public const string Time = "Edm.TimeOfDay";
    public const string Duration = "Edm.Duration";
    public const string TimeOfDay = "Edm.TimeOfDay";
    public const string Guid = "Edm.Guid";
    public const string Decimal = "Edm.Decimal";
    public const string Double = "Edm.Double";
    public const string Single = "Edm.Single";
    public const string Short = "Edm.Int16";
    public const string Int = "Edm.Int32";
    public const string Long = "Edm.Int64";
    public const string UShort = "Edm.UInt16";
    public const string UInt = "Edm.UInt32";
    public const string ULong = "Edm.UInt64";
    public const string Byte = "Edm.Byte";
    public const string SByte = "Edm.SByte";
    public const string Binary = "Edm.Binary";
    public const string Stream = "Edm.Stream";
    public const string Geography = "Edm.Geography";
    public const string GeographyPoint = "Edm.GeographyPoint";
    public const string GeographyLineString = "Edm.GeographyLineString";
    public const string GeographyPolygon = "Edm.GeographyPolygon";
    public const string GeographyMultiPoint = "Edm.GeographyMultiPoint";
    public const string GeographyMultiLineString = "Edm.GeographyMultiLineString";
    public const string GeographyMultiPolygon = "Edm.GeographyMultiPolygon";
    public const string GeographyCollection = "Edm.GeographyCollection";
    public const string Geometry = "Edm.Geometry";
    public const string GeometryPoint = "Edm.GeometryPoint";
    public const string GeometryLineString = "Edm.GeometryLineString";
    public const string GeometryPolygon = "Edm.GeometryPolygon";
    public const string GeometryMultiPoint = "Edm.GeometryMultiPoint";
    public const string GeometryMultiLineString = "Edm.GeometryMultiLineString";
    public const string GeometryMultiPolygon = "Edm.GeometryMultiPolygon";
    public const string GeometryCollection = "Edm.GeometryCollection";
}
