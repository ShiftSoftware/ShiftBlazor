using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ShiftBlazor.Components.Pii;

internal sealed class PiiFieldQueryPolicy(IServiceProvider services) : IFieldQueryPolicy
{
    private static readonly FieldQueryRule Exact = new([ODataOperator.Equal], false);
    private static readonly FieldQueryRule Partial = new([ODataOperator.Contains, ODataOperator.Equal, ODataOperator.StartsWith, ODataOperator.EndsWith], false);
    private static readonly FieldQueryRule Unavailable = new([], false);

    public FieldQueryRule? GetRule(Type dtoType, string path)
    {
        foreach (var segment in path.Replace('/', '.').Split('.'))
        {
            var property = dtoType.GetProperty(segment);
            if (property is null) return null;
            if (property.PropertyType == typeof(PiiFieldDTO))
            {
                if (PiiFieldProtection.FindDeclaration(property)?.Revealable != true) return Unavailable;
                var action = services.GetService<IOptions<PiiOptions>>()?.Value.PartialSearchAction;
                return action is not null && services.GetService<ITypeAuthService>()?.CanAccess(action) == true ? Partial : Exact;
            }
            dtoType = property.PropertyType;
        }
        return null;
    }
}
