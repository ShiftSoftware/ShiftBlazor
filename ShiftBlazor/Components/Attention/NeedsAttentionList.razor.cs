namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>
/// Cross-entity Blazor component that aggregates active attention signals across all
/// <see cref="ShiftSoftware.ShiftEntity.Core.Attention.IHasIndexedAttention"/> entities.
/// Fetches from a standalone endpoint and renders a table with severity, entity type,
/// category, reason, timestamps, and per-row open-actions (popup, new tab, same tab).
/// Entities using JSON-shadow storage do not appear here by design.
/// </summary>
public partial class NeedsAttentionList { }
