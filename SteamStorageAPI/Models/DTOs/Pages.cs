using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Pages;

namespace SteamStorageAPI.Models.DTOs;

public record PageResponse(
    int Id,
    string Title);

public record PagesResponse(
    int Count,
    IEnumerable<PageResponse> Pages);

[Validator<SetPageRequestValidator>]
public record SetPageRequest(
    int PageId);