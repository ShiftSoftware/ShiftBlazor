using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

internal static class RevisionExtensions
{
    /// <summary>
    /// Whether this is the open-ended (current) revision.
    /// <para>
    /// SQL Server marks an open period with 9999-12-31 23:59:59.9999999, but that sentinel does not
    /// survive the trip here as an instant: the API stamps it with the <em>server's</em> UTC offset
    /// and the client may restamp it with the <em>browser's</em>, so the two machines disagree on
    /// what instant it is. Comparing against <see cref="DateTime.MaxValue"/> is worse than
    /// unreliable — converting that constant to a <see cref="DateTimeOffset"/> throws outright in
    /// any timezone behind UTC. The year is the one part every offset leaves intact, and no real
    /// revision is dated 9999.
    /// </para>
    /// </summary>
    internal static bool IsCurrent(this RevisionDTO revision)
        => revision.ValidTo is not { } validTo || validTo.UtcDateTime.Year == DateTime.MaxValue.Year;

    /// <summary>
    /// The <c>asOf</c> this revision's snapshot is fetched with, or <c>null</c> for the current
    /// revision, which is read live. A live read is not the same request as an <c>asOf</c> read of
    /// the same row — a temporal query drops includes that reach non-temporal tables — so the
    /// distinction has to be kept.
    /// </summary>
    internal static DateTimeOffset? AsOf(this RevisionDTO revision)
        => revision.IsCurrent() ? null : revision.ValidFrom;
}
