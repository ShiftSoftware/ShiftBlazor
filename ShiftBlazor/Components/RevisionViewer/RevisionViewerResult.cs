using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>A single revision the user picked to view; AsOf is null for the live record.</summary>
internal sealed record ViewRevision(DateTimeOffset? AsOf);

/// <summary>The two revisions to compare, ordered oldest-first (Old then New).</summary>
public sealed record CompareRevisions(RevisionDTO Old, RevisionDTO New);
