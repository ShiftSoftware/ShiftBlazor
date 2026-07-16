namespace ShiftSoftware.ShiftBlazor.Utils;

/// <summary>
/// Diagnostics for ShiftList data loads.
/// </summary>
public static class ShiftListPerfProbe
{
    /// <summary>
    /// Logs per-phase timings (network / deserialize / render) of every list load to the browser
    /// console, so a hitch can be attributed to the exact phase. Tied to the DEBUG compilation
    /// symbol rather than a manually-flipped flag, so it can't accidentally ship enabled in a
    /// Release build: in project-reference (development) mode MSBuild builds this project under
    /// the referencing app's own configuration, and in package mode the published NuGet build is
    /// Release — either way "false" here is automatic, not something to remember to set.
    /// </summary>
#if DEBUG
    public const bool LogTimings = true;
#else
    public const bool LogTimings = false;
#endif
}
