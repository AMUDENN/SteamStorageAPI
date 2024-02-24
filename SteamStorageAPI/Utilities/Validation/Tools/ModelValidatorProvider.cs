using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SteamStorageAPI.Utilities.Validation.Tools;

public sealed class ModelValidatorProvider : IModelValidatorProvider
{
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (context.ModelMetadata is not DefaultModelMetadata m || m.Attributes.TypeAttributes is null)
            return;

        Type? validatorType = FindValidatorType(m.Attributes.Attributes);
        if (validatorType is null)
            return;

        context.Results.Add(new()
        {
            Validator = new ModelValidator(validatorType),
            IsReusable = false
        });
    }

    private static Type? FindValidatorType(IEnumerable<object> attributes)
    {
        Attribute? attribute = attributes.OfType<Attribute>()
            .FirstOrDefault(attribute => attribute.GetType()
                .GenericTypeArguments.Any(type => type.GetInterfaces()
                    .Contains(typeof(IValidator))));
        return attribute?.GetType().GenericTypeArguments.First();
    }
}
