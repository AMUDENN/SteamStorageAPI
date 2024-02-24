using FluentValidation;

namespace SteamStorageAPI.Utilities.Validation.Tools;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidatorAttribute<T> : Attribute where T : IValidator;
