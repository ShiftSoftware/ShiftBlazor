namespace ShiftSoftware.ShiftBlazor.Filters;

/// <summary>Lets a column supply its server member path and effective filter operator.</summary>
public interface IQueryColumn
{
    string? QueryPath { get; }
    string? GetQueryOperator(string? requested);
}
