using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SteamStorageAPI.Utilities.Validation.Tools;

public sealed class ModelValidator : IModelValidator
{
    #region Fields

    private readonly Type _validatorType;

    #endregion Fields

    #region Constructor

    public ModelValidator(Type validatorType)
    {
        _validatorType = validatorType;
    }

    #endregion Constructor

    #region Methods

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model is null || Activator.CreateInstance(_validatorType) is not IValidator validator)
            return [];

        ValidationResult? result = validator.Validate(GetValidationContext(context.Model));

        return result.IsValid
            ? []
            : result.Errors.Select(error => new ModelValidationResult(error.PropertyName, error.ErrorMessage));
    }

    private static IValidationContext GetValidationContext(object model)
    {
        Type genericType = typeof(ValidationContext<>).MakeGenericType(model.GetType());
        ConstructorInfo constructor =
            genericType.GetConstructors().FirstOrDefault(constructor => constructor.GetParameters().Length == 1)
            ?? throw new ArgumentException($"Could not find a constructor with 1 parameter of type `{genericType}`.",
                nameof(constructor));

        return (IValidationContext)constructor.Invoke([model]);
    }

    #endregion Methods
}