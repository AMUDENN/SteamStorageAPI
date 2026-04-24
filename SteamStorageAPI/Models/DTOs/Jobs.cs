using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Jobs;

namespace SteamStorageAPI.Models.DTOs;

[Validator<TriggerJobRequestValidator>]
public record TriggerJobRequest(
    JobName JobName);