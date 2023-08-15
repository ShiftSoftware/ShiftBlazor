using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class FormHelper
    {
        public static bool IsRequired<T>(Expression<Func<T>> _for)
        {
            try
            {
                var memberExpression = (MemberExpression)_for!.Body;
                var type = memberExpression.Expression?.Type;
                var name = memberExpression.Member.Name;

                var isRequired = (type?.GetProperty(name))?.GetCustomAttribute<RequiredAttribute>() != null;
                
                if (isRequired) return isRequired;

                var assemblyScanner = AssemblyScanner.FindValidatorsInAssembly(type?.Assembly).FirstOrDefault();
                if (assemblyScanner != null)
                {
                    var validator = Activator.CreateInstance(assemblyScanner.ValidatorType) as IValidator;
                    var propertyRule = validator?
                            .CreateDescriptor()
                            .GetRulesForMember(name)
                            .FirstOrDefault();
                    var RuleTypes = propertyRule?.Components.Select(x => x.Validator.GetType());

                    isRequired = RuleTypes?.Any(x => x.Name.StartsWith("NotEmpty") || x.Name.StartsWith("NotNull")) ?? false;
                }

                return isRequired;

            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
