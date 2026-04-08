using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.ArchiveGroups;

public sealed record ArchiveGroupDto(
    int Id,
    string Title,
    string? Description,
    string Colour,
    int Count,
    decimal BuySum,
    decimal SoldSum,
    double Change,
    DateTime DateCreation);

public sealed record ArchiveGroupsStatisticDto(
    int ArchivesCount,
    decimal InvestmentSum,
    decimal SoldSum,
    IReadOnlyList<GameShareDto> GameCount,
    IReadOnlyList<GameShareDto> GameInvestmentSum,
    IReadOnlyList<GameShareDto> GameSoldSum);

public sealed record GameShareDto(
    string GameTitle,
    double Percentage,
    decimal Value);

public sealed record CreateArchiveGroupDto(
    int UserId,
    string Title,
    string? Description,
    string? Colour);

public sealed record UpdateArchiveGroupDto(
    int GroupId,
    int UserId,
    string Title,
    string? Description,
    string? Colour);

public sealed record GetArchiveGroupsFilterDto(
    ArchiveGroupOrderName? OrderName,
    bool? IsAscending);
