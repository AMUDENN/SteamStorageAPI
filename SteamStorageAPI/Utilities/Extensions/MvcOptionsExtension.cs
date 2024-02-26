using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Utilities.Validation.Tools;

namespace SteamStorageAPI.Utilities.Extensions;

public static class MvcOptionsExtension
{
    public static MvcOptions AddAutoValidation(this MvcOptions options)
    {
        options.ModelValidatorProviders.Add(new ModelValidatorProvider());
        return options;
    }
}
