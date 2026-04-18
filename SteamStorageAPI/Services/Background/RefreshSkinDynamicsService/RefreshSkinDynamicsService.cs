using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Background.RefreshSkinDynamicsService;

public class RefreshSkinDynamicsService : IRefreshSkinDynamicsService
{
    #region Constants

    // Steam Market allows ~200 req/5min per IP; 1 req/4-6s keeps us well below that
    private const int PAGE_SIZE = 100;

    private const int DELAY_MIN_MS = 4_000;
    private const int DELAY_MAX_MS = 6_000;

    // Exponential backoff on rate limiting: 2min → 4min → 8min → cap 10min
    private const int BACKOFF_BASE_MS = 120_000;
    private const int BACKOFF_MAX_MS  = 600_000;

    #endregion Constants

    #region Fields

    private readonly ILogger<RefreshSkinDynamicsService> _logger;
    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshSkinDynamicsService(
        ILogger<RefreshSkinDynamicsService> logger,
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _logger = logger;
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshSkinDynamicsAsync(CancellationToken cancellationToken = default)
    {
        Currency baseCurrency =
            await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == Currency.BASE_CURRENCY_ID, cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                "The base currency is missing from the database");

        List<Game> games = await _context.Games.ToListAsync(cancellationToken);

        using HttpClient client = _httpClientFactory.CreateClient();

        foreach (Game game in games)
            await ProcessGameAsync(client, game, baseCurrency, cancellationToken);
    }

    private async Task ProcessGameAsync(
        HttpClient client,
        Game game,
        Currency baseCurrency,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting skin update for game {GameId}", game.SteamGameId);

        HashSet<string> existingHashNames =
            [.. await _context.Skins
                .Where(x => x.GameId == game.Id)
                .Select(x => x.MarketHashName)
                .ToListAsync(cancellationToken)];

        int start = 0;
        int totalCount = int.MaxValue;
        int consecutiveErrors = 0;
        Stopwatch stopwatch = new();

        while (start < totalCount)
        {
            cancellationToken.ThrowIfCancellationRequested();

            stopwatch.Restart();

            (SteamSkinResponse? response, TimeSpan retryAfter) =
                await FetchPageAsync(client, game.SteamGameId, baseCurrency.SteamCurrencyId, start, cancellationToken);

            stopwatch.Stop();

            if (response is null)
            {
                consecutiveErrors++;
                int backoffMs = ComputeBackoffMs(retryAfter, consecutiveErrors);
                _logger.LogWarning(
                    "Steam request failed for game {GameId} (start={Start}, error #{ErrorCount}). Retrying in {BackoffMs} ms",
                    game.SteamGameId, start, consecutiveErrors, backoffMs);
                await Task.Delay(backoffMs, cancellationToken);
                continue;
            }

            consecutiveErrors = 0;
            totalCount = response.total_count;

            SkinResult[] results = response.results ?? [];

            if (results.Length == 0)
                break;

            await SaveNewSkinsAsync(game, results, existingHashNames, cancellationToken);
            await SaveSkinDynamicsAsync(results, cancellationToken);

            start += results.Length;

            _logger.LogInformation(
                "Game {GameId}: {Start}/{Total} skins processed ({ElapsedMs} ms)",
                game.SteamGameId, start, totalCount, stopwatch.ElapsedMilliseconds);

            await Task.Delay(Random.Shared.Next(DELAY_MIN_MS, DELAY_MAX_MS), cancellationToken);
        }

        _logger.LogInformation(
            "Skin update for game {GameId} completed. Total: {Total}",
            game.SteamGameId, totalCount == int.MaxValue ? 0 : totalCount);
    }

    private async Task<(SteamSkinResponse? Response, TimeSpan RetryAfter)> FetchPageAsync(
        HttpClient client,
        int appId,
        int currencyId,
        int start,
        CancellationToken cancellationToken)
    {
        string url = _steamApiUrlBuilder.GetSkinsUrl(appId, currencyId, PAGE_SIZE, start);

        try
        {
            using HttpResponseMessage httpResponse = await client.GetAsync(url, cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan retryAfter = httpResponse.Headers.RetryAfter?.Delta ?? TimeSpan.Zero;
                _logger.LogWarning("Steam 429 Too Many Requests (start={Start}). Retry-After: {RetryAfter}", start, retryAfter);
                return (null, retryAfter);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Steam returned {StatusCode} (start={Start})", (int)httpResponse.StatusCode, start);
                return (null, TimeSpan.Zero);
            }

            SteamSkinResponse? response =
                await httpResponse.Content.ReadFromJsonAsync<SteamSkinResponse>(cancellationToken);

            if (response is null || !response.success)
            {
                _logger.LogWarning("Steam returned success=false or null body (start={Start})", start);
                return (null, TimeSpan.Zero);
            }

            return (response, TimeSpan.Zero);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Covers network failures and HTTP timeouts (which surface as TaskCanceledException
            // with a different token, not the user's cancellationToken)
            _logger.LogWarning("HTTP request to Steam failed (start={Start}): {Message}", start, ex.Message);
            return (null, TimeSpan.Zero);
        }
    }

    private async Task SaveNewSkinsAsync(
        Game game,
        SkinResult[] results,
        HashSet<string> existingHashNames,
        CancellationToken cancellationToken)
    {
        List<Skin> newSkins = results
            .Where(x => x.hash_name is not null && !existingHashNames.Contains(x.hash_name))
            .Select(x => new Skin
            {
                GameId = game.Id,
                MarketHashName = x.hash_name!,
                Title = x.name ?? string.Empty,
                SkinIconUrl = x.asset_description?.icon_url ?? string.Empty
            })
            .ToList();

        if (newSkins.Count == 0)
            return;

        await _context.Skins.AddRangeAsync(newSkins, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (Skin skin in newSkins)
            existingHashNames.Add(skin.MarketHashName);

        _logger.LogInformation(
            "Added {Count} new skins for game {GameId}: {Names}",
            newSkins.Count, game.SteamGameId,
            string.Join(", ", newSkins.Select(x => x.MarketHashName)));
    }

    private async Task SaveSkinDynamicsAsync(
        SkinResult[] results,
        CancellationToken cancellationToken)
    {
        List<string> hashNames = results
            .Where(x => x.hash_name is not null)
            .Select(x => x.hash_name!)
            .ToList();

        Dictionary<string, int> skinIds = await _context.Skins
            .Where(x => hashNames.Contains(x.MarketHashName))
            .Select(x => new { x.MarketHashName, x.Id })
            .ToDictionaryAsync(x => x.MarketHashName, x => x.Id, cancellationToken);

        List<SkinsDynamic> dynamics = results
            .Where(x => x.hash_name is not null
                     && skinIds.ContainsKey(x.hash_name)
                     && x.sell_price > 0)
            .Select(x => new SkinsDynamic
            {
                DateUpdate = DateTime.UtcNow,
                Price = x.sell_price / 100.0m,
                SkinId = skinIds[x.hash_name!]
            })
            .ToList();

        if (dynamics.Count == 0)
            return;

        await _context.SkinsDynamics.AddRangeAsync(dynamics, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static int ComputeBackoffMs(TimeSpan retryAfter, int consecutiveErrors)
    {
        if (retryAfter > TimeSpan.Zero)
            return (int)retryAfter.TotalMilliseconds;

        int backoffMs = (int)Math.Min(BACKOFF_BASE_MS * Math.Pow(2, consecutiveErrors - 1), BACKOFF_MAX_MS);
        int jitter = Random.Shared.Next(0, backoffMs / 5);
        return backoffMs + jitter;
    }

    #endregion Methods
}
