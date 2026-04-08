using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Roles;

namespace SteamStorageAPI.Models.DTOs;

public record RoleResponse(
    int Id,
    string Title);

public record RolesResponse(
    int Count,
    IEnumerable<RoleResponse> Roles);

[Validator<SetRoleRequestValidator>]
public record SetRoleRequest(
    int UserId,
    int RoleId);