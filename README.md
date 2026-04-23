# 🎮 SteamStorage

A self-hosted investment tracker for Steam Market items. Build portfolios of active and archived skin positions, track price dynamics, monitor aggregated statistics — all against live Steam Market data.

---

## 📦 Projects

| Project | Description | Default port |
|---|---|---|
| `SteamStorageAPI` | REST API — core backend | `8081` |
| `LoginWebApp` | Steam OpenID login flow | `8083` |
| `AdminPanel` | Admin dashboard (currencies, games, users, jobs) | `8085` |

Supporting services: **Prometheus** (`9090`) · **Grafana** (`3000`)

---

## 🏗️ Architecture

### High-level overview

```
Browser
  ├── LoginWebApp   ──► Steam OpenID ──► issues JWT
  ├── AdminPanel    ──► SteamStorageAPI (internal HTTP + cookie JWT)
  └── Client App    ──► SteamStorageAPI (JWT Bearer)

SteamStorageAPI
  ├── SQL Server  (EF Core 10, code-first migrations)
  ├── Quartz.NET background jobs
  │     ├── RefreshSkinDynamicsJob          — 00:00 daily
  │     ├── RefreshCurrenciesJob            — 01:00 daily
  │     └── RefreshActiveGroupsDynamicsJob  — 03:00 daily
  └── OpenTelemetry ──► Prometheus ──► Grafana
```

---

### 🔐 Authentication flow

```
User browser
  │
  ├─[1]─► GET /api/Authorize/GetAuthUrl
  │          └── returns Steam OpenID redirect URL
  │
  ├─[2]─► Redirect to Steam login page
  │          └── Steam calls back to LoginWebApp
  │
  ├─[3]─► LoginWebApp validates Steam OpenID assertion
  │          └── calls SteamStorageAPI /api/Authorize/SteamAuthCallback
  │              └── creates/updates user, issues short-lived auth code
  │
  └─[4]─► GET /api/Authorize/ExchangeToken?authCode=<code>
             └── returns JWT Bearer token (valid N days, configurable)
```

All subsequent requests carry `Authorization: Bearer <jwt>`.  
The token contains three claims: `SteamId`, `UserId`, and `Role`.

---

### 📁 SteamStorageAPI — project layout

```
SteamStorageAPI/
├── Controllers/               15 API controllers
├── Models/
│   ├── DBEntities/            EF Core entity classes + SteamStorageContext
│   ├── DTOs/                  Request/response records, pagination, enums
│   └── SteamAPIModels/        Steam Web API response models
├── Services/
│   ├── Domain/                Business logic (one service per aggregate)
│   ├── Background/            Quartz jobs + per-job service implementations
│   └── Infrastructure/        Cross-cutting: JWT, user context, Steam URL builder
├── Middlewares/               RequestLoggingMiddleware
├── Utilities/
│   ├── Config/                AppConfig + YAML reader (YamlDotNet)
│   ├── ExceptionHandlers/     GlobalExceptionHandler (IExceptionHandler)
│   ├── Extensions/            Service-registration extension methods
│   ├── HealthCheck/           Custom IHealthCheck implementations
│   ├── JWT/                   JwtOptions
│   └── Validation/            FluentValidation validators + shared constants
├── Migrations/                EF Core migration files
├── Program.cs                 Entry point, DI wiring, middleware pipeline
├── Dockerfile
└── .config.yaml               Runtime configuration (secrets, cron, DB, JWT)
```

---

### 🔄 Request lifecycle

Every incoming HTTP request passes through the following pipeline in order:

```
Incoming request
  │
  ▼
ForwardedHeaders         — trust X-Forwarded-For/Proto from reverse proxy
  │
  ▼
IpRateLimiting           — enforce per-IP rate limits (AspNetCoreRateLimit)
  │
  ▼
ExceptionHandler         — GlobalExceptionHandler catches all unhandled exceptions
  │                         HttpResponseException → custom status code
  │                         OperationCanceledException → 499
  │                         anything else → 500
  ▼
Authentication           — validate JWT Bearer token, populate ClaimsPrincipal
  │
  ▼
Authorization            — enforce [Authorize] / role policies
  │
  ▼
RequestLoggingMiddleware — log endpoint name + elapsed ms (Stopwatch)
  │
  ▼
Controller / Endpoint
  │  ├── FluentValidation (auto-validates request DTO before action executes)
  │  ├── Domain Service   (business logic, queries EF Core DbContext)
  │  └── return result
  │
  ▼
Response
```

---

### 🧩 Service layers

#### Domain services (Scoped)

One service per business aggregate, injected into controllers via constructor DI.

| Service | Responsibility |
|---|---|
| `AuthorizeService` | Steam OpenID assertion validation, user upsert, auth-code lifecycle, JWT issuance |
| `UserService` | User profile reads/writes, goal sum, admin access check |
| `RoleService` | Role enumeration and assignment |
| `PageService` | Available pages, user start-page preference |
| `CurrencyService` | Currency CRUD, exchange-rate history, user currency selection |
| `GameService` | Steam game metadata CRUD |
| `SkinService` | Skin CRUD, price-history reads, marked-skin management |
| `InventoryService` | Steam inventory sync, local inventory reads, statistics |
| `ActiveGroupService` | Active-group CRUD, aggregated stats, price-dynamics history |
| `ActiveService` | Active-position CRUD, sell action (moves to archive), statistics |
| `ArchiveGroupService` | Archive-group CRUD, aggregated stats |
| `ArchiveService` | Archive-position CRUD, statistics |
| `StatisticsService` | Cross-aggregate analytics (investment sum, goal progress, counts by game) |
| `FileService` | Excel export via EPPlus — dumps portfolio to `.xlsx` |

#### Infrastructure services (Scoped)

| Service | Responsibility |
|---|---|
| `JwtProvider` | Generates signed JWT with `SteamId`, `UserId`, `Role` claims |
| `ContextUserService` | Reads `UserId` from `ClaimsPrincipal`, loads and caches the `User` entity for the request |
| `SteamApiUrlBuilder` | Builds parameterised Steam Web API URLs (inventory, market prices, user profiles) |

---

### 🗄️ Data model

`SteamStorageContext` (EF Core 10, SQL Server) manages 15 entities:

```
User ────────── Role
 │  └────────── Currency ─── CurrencyDynamic
 │
 ├── ActiveGroup ──────────── Active ────────── Skin ─── SkinsDynamic
 │    └── ActiveGroupsDynamic                    │
 │                                               │ (shared across users)
 ├── ArchiveGroup ─────────── Archive ──────────┘
 │
 ├── Inventory ──────────────────────────────── Skin
 └── MarkedSkin ─────────────────────────────── Skin
```

Key schema decisions:

- `decimal(14,2)` for market prices; `decimal(14,4)` for exchange rates
- `DateUpdate` / `DateCreation` on all history and group tables for audit trails
- Composite indexes on `(SkinId, DateUpdate)`, `(CurrencyId, DateUpdate)`, `(GroupId, DateUpdate)` — used by dynamics queries
- A SQL trigger `UpdateSkinsCurrentPrice` on `SkinsDynamic` keeps `Skin.CurrentPrice` in sync without a separate UPDATE query
- `SaveChangesAsync` is the implicit transaction boundary — each service method is one atomic unit of work

---

### ⚙️ Background jobs

Jobs are Quartz.NET `IJob` implementations. Because Quartz registers jobs as singletons but EF Core `DbContext` is Scoped, each job execution opens its own DI scope via `IServiceScopeFactory.CreateScope()`:

```
Quartz scheduler (Singleton)
  └── IJob.Execute()
        └── IServiceScopeFactory.CreateScope()
              └── scope.ServiceProvider.GetRequiredService<IRefreshXyzService>()
                    └── uses SteamStorageContext (Scoped — safe inside the scope)
```

Back-off on Steam API rate-limit errors: `delay = clamp(BASE_MS × 2^(errors−1), 0, MAX_MS)`, capped at 10 min.

| Job | Cron | What it does |
|---|---|---|
| `RefreshSkinDynamicsJob` | `0 0 0 * * ?` | Fetches current prices for all tracked skins from Steam Market, stores `SkinsDynamic` rows |
| `RefreshCurrenciesJob` | `0 0 1 * * ?` | Pulls latest exchange rates, stores `CurrencyDynamic` rows |
| `RefreshActiveGroupsDynamicsJob` | `0 0 3 * * ?` | Recalculates totals for every `ActiveGroup`, stores `ActiveGroupsDynamic` rows |

---

### ✅ Validation

Every request DTO has a corresponding **FluentValidation** validator.  
Validators are auto-run before the action executes via a custom `IModelValidatorProvider` — the controller never calls `ModelState.IsValid` manually.  
Shared numeric limits (e.g. `MaxPrice = 1 000 000 000 000`) live in `ValidationConstants` and are imported by all relevant validators.

---

### 📊 Observability pipeline

```
SteamStorageAPI
  └── OpenTelemetry SDK
        ├── HTTP server metrics  (latency, status codes, routes)
        ├── HttpClient metrics   (Steam API call duration, errors)
        └── .NET Runtime metrics (GC, thread pool, memory)
              │
              ▼
        /api/metrics  (Bearer <internalApiKey> required)
              │
              ▼
           Prometheus  (scrape interval 15 s, retention 30 days)
              │
              ▼
            Grafana  (data source → http://prometheus:9090)
```

---

## 🚀 Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker & Docker Compose
- SQL Server (or point the connection string at any instance)
- A [Steam Web API key](https://steamcommunity.com/dev/apikey)

### 1. Create config files from examples

```bash
cp SteamStorageAPI/.config.yaml.example  SteamStorageAPI/.config.yaml
cp LoginWebApp/.config.yaml.example      LoginWebApp/.config.yaml
cp prometheus.yml.example                prometheus.yml
cp grafana.env.example                   grafana.env
```

### 2. Fill in `SteamStorageAPI/.config.yaml`

```yaml
app:
  tokenAddress: "https://<your-domain>/token/"
  dateFormat: "dd.MM.yyyy"
  publicHost: "localhost:8081"
  internalApiKey: "<random-secret>"        # shared with Prometheus and AdminPanel

jwt:
  key: "<min-32-char-random-string>"
  issuer: "YourIssuer"
  audience: "SteamStorageUser"
  expiresDays: 1

steam:
  apiKey: "<steam-web-api-key>"

database:
  steamStorage: "Server=<host>,1433;Database=SteamStorage;User Id=<user>;Password=<pass>;TrustServerCertificate=True"

healthChecks:
  apiUrl: "http://steamstorage.api"
  adminPanelUrl: "http://adminpanel"
  loginWebAppUrl: "http://loginwebapp"

backgroundServices:
  refreshSkinDynamicsJob:
    cronSchedule: "0 0 0 * * ?"
    errorDelayMs: 1800000
  refreshCurrencies:
    cronSchedule: "0 0 1 * * ?"
    errorDelayMs: 1800000
  refreshActiveGroupsDynamicsJob:
    cronSchedule: "0 0 3 * * ?"
    errorDelayMs: 1800000
```

### 3. Fill in `LoginWebApp/.config.yaml`

```yaml
app:
  internalApiKey: "<same-value-as-above>"
```

### 4. Fill in `prometheus.yml`

Set `credentials` to the same value as `internalApiKey`:

```yaml
authorization:
  type: Bearer
  credentials: <your-internal-api-key>
```

### 5. Apply database migrations

```bash
cd SteamStorageAPI
dotnet ef database update
```

### 6. Run with Docker Compose

```bash
docker compose up --build
```

| URL | Description |
|---|---|
| `http://localhost:8081/api/swagger` | Swagger UI (dev only) |
| `http://localhost:8083/` | Login page |
| `http://localhost:8085/` | Admin panel |
| `http://localhost:9090/` | Prometheus |
| `http://localhost:3000/` | Grafana |

---

## 📡 API reference

All endpoints require `Authorization: Bearer <jwt>` unless noted otherwise.  
Full interactive docs (request/response schemas, try-it-out) available at `/api/swagger` in development mode.

---

### 🔑 Authorization

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/Authorize/GetAuthUrl` | — | Returns the Steam OpenID redirect URL to send the user to |
| `GET` | `/api/Authorize/SteamAuthCallback` | — | Steam calls this after login; creates/updates the user record and issues a short-lived auth code |
| `GET` | `/api/Authorize/ExchangeToken` | — | Exchange an auth code for a JWT Bearer token |

---

### 📂 Active groups

Open-position containers. Each active item belongs to exactly one group.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/ActiveGroups/GetActiveGroupInfo` | Get a single group (id, title, colour, statistics) |
| `GET` | `/api/ActiveGroups/GetActiveGroups` | List all active groups for the current user |
| `GET` | `/api/ActiveGroups/GetActiveGroupsStatistic` | Aggregated stats across all active groups (invested, current value, change %) |
| `GET` | `/api/ActiveGroups/GetActiveGroupDynamics` | Price-history series for a group (used for charts) |
| `GET` | `/api/ActiveGroups/GetActiveGroupsCount` | Total number of active groups |
| `POST` | `/api/ActiveGroups/PostActiveGroup` | Create a new active group |
| `PUT` | `/api/ActiveGroups/PutActiveGroup` | Update group title / colour |
| `DELETE` | `/api/ActiveGroups/DeleteActiveGroup` | Delete group (and all its items) |

---

### 💼 Actives (open positions)

Individual skin positions that are currently held.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Actives/GetActiveInfo` | Get a single active item (skin, count, buy price, current price, PnL) |
| `GET` | `/api/Actives/GetActives` | Paginated list of active items with optional filters |
| `GET` | `/api/Actives/GetActivesStatistic` | Aggregated statistics for a group or the whole portfolio |
| `GET` | `/api/Actives/GetActivesPagesCount` | Number of pages for the current filter/page-size combination |
| `GET` | `/api/Actives/GetActivesCount` | Total count of active items |
| `POST` | `/api/Actives/PostActive` | Add a new active position |
| `PUT` | `/api/Actives/PutActive` | Update count, buy price, or description |
| `PUT` | `/api/Actives/SoldActive` | Mark item as sold — moves it to the archive with a sell price and date |
| `DELETE` | `/api/Actives/DeleteActive` | Delete an active position |

---

### 🗄️ Archive groups

Closed-position containers. Each archived item belongs to exactly one archive group.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/ArchiveGroups/GetArchiveGroupInfo` | Get a single archive group |
| `GET` | `/api/ArchiveGroups/GetArchiveGroups` | List all archive groups for the current user |
| `GET` | `/api/ArchiveGroups/GetArchiveGroupsStatistic` | Aggregated stats across all archive groups (invested, sold for, profit) |
| `GET` | `/api/ArchiveGroups/GetArchiveGroupsCount` | Total number of archive groups |
| `POST` | `/api/ArchiveGroups/PostArchiveGroup` | Create a new archive group |
| `PUT` | `/api/ArchiveGroups/PutArchiveGroup` | Update group title / colour |
| `DELETE` | `/api/ArchiveGroups/DeleteArchiveGroup` | Delete group (and all its items) |

---

### 📦 Archives (closed positions)

Individual skin positions that have been sold.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Archives/GetArchiveInfo` | Get a single archived item (skin, count, buy price, sell price, profit) |
| `GET` | `/api/Archives/GetArchives` | Paginated list of archived items with optional filters |
| `GET` | `/api/Archives/GetArchivesStatistic` | Aggregated profit/loss statistics |
| `GET` | `/api/Archives/GetArchivesPagesCount` | Number of pages for the current filter/page-size combination |
| `GET` | `/api/Archives/GetArchivesCount` | Total count of archived items |
| `POST` | `/api/Archives/PostArchive` | Manually add an archived position |
| `PUT` | `/api/Archives/PutArchive` | Update count, buy price, sell price, or description |
| `DELETE` | `/api/Archives/DeleteArchive` | Delete an archived position |

---

### 💱 Currencies

Fiat currencies used for price display. Exchange rates are refreshed daily.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Currencies/GetCurrencies` | List all available currencies |
| `GET` | `/api/Currencies/GetCurrency` | Get a single currency (id, title, mark, steam currency id) |
| `GET` | `/api/Currencies/GetCurrentCurrency` | Get the current user's selected currency |
| `GET` | `/api/Currencies/GetCurrencyDynamics` | Exchange-rate history series for a currency |
| `POST` | `/api/Currencies/PostCurrency` | *(Admin)* Add a new currency |
| `PUT` | `/api/Currencies/PutCurrencyInfo` | *(Admin)* Update currency metadata |
| `PUT` | `/api/Currencies/SetCurrency` | Set the current user's preferred display currency |
| `DELETE` | `/api/Currencies/DeleteCurrency` | *(Admin)* Remove a currency |

---

### 🎮 Games

Steam games whose items can be tracked (CS2, Dota 2, etc.).

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Games/GetGames` | List all games |
| `POST` | `/api/Games/PostGame` | *(Admin)* Add a new game |
| `PUT` | `/api/Games/PutGameInfo` | *(Admin)* Update game metadata |
| `DELETE` | `/api/Games/DeleteGame` | *(Admin)* Remove a game |

---

### 🎨 Skins

Steam Market items. Skins are shared across users; only the positions (actives/archives) are per-user.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Skins/GetSkinInfo` | Get a single skin (name, game, current market price) |
| `GET` | `/api/Skins/GetBaseSkins` | List base skins (unfiltered, used for autocomplete) |
| `GET` | `/api/Skins/GetSkins` | Paginated skin list with search/filter |
| `GET` | `/api/Skins/GetSkinDynamics` | Price-history series for a skin |
| `GET` | `/api/Skins/GetSkinPagesCount` | Number of pages for the current filter/page-size |
| `GET` | `/api/Skins/GetSteamSkinsCount` | Total number of skins available on the Steam Market for a game |
| `GET` | `/api/Skins/GetSavedSkinsCount` | Number of skins currently saved in the local database |
| `POST` | `/api/Skins/PostSkin` | *(Admin)* Add a skin to the database |
| `PUT` | `/api/Skins/SetMarkedSkin` | Mark/unmark a skin as a favourite |
| `DELETE` | `/api/Skins/DeleteMarkedSkin` | Remove a skin from favourites |

---

### 🎒 Inventory

The current user's Steam inventory items.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Inventory/GetInventory` | Paginated list of inventory items with their current market prices |
| `GET` | `/api/Inventory/GetInventoriesStatistic` | Aggregated inventory value statistics |
| `GET` | `/api/Inventory/GetInventoryPagesCount` | Number of pages for the current filter/page-size |
| `GET` | `/api/Inventory/GetSavedInventoriesCount` | Number of inventory items saved locally |
| `POST` | `/api/Inventory/RefreshInventory` | Fetch the latest inventory from Steam and sync to DB |

---

### 📊 Statistics

Cross-cutting statistics endpoints.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Statistics/GetInvestmentSum` | Total amount invested across all active positions |
| `GET` | `/api/Statistics/GetFinancialGoal` | Current portfolio value vs. the user's financial goal |
| `GET` | `/api/Statistics/GetActiveStatistic` | Detailed active-portfolio statistics (invested, current value, PnL, change %) |
| `GET` | `/api/Statistics/GetArchiveStatistic` | Detailed archive statistics (total invested, total sold, profit) |
| `GET` | `/api/Statistics/GetInventoryStatistic` | Inventory valuation statistics |
| `GET` | `/api/Statistics/GetItemsCount` | Total item count across actives and archives |
| `GET` | `/api/Statistics/GetUsersCountByCurrency` | *(Admin)* Number of users per currency |
| `GET` | `/api/Statistics/GetItemsCountByGame` | *(Admin)* Item count broken down by game |

---

### 👤 Users

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Users/GetUsers` | *(Admin)* Paginated user list with search by id / nickname / Steam id |
| `GET` | `/api/Users/GetUsersCount` | *(Admin)* Total number of registered users |
| `GET` | `/api/Users/GetUserInfo` | *(Admin)* Get any user's profile by id |
| `GET` | `/api/Users/GetCurrentUserInfo` | Get the current user's profile (Steam nickname, avatar, role, currency) |
| `GET` | `/api/Users/GetCurrentUserGoalSum` | Get the current user's financial goal target amount |
| `GET` | `/api/Users/GetHasAccessToAdminPanel` | Check whether the current user has admin access |
| `PUT` | `/api/Users/PutGoalSum` | Update the current user's financial goal target |
| `DELETE` | `/api/Users/DeleteUser` | *(Admin)* Delete a user account |

---

### 🔖 Roles

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Roles/GetRoles` | List all available roles |
| `PUT` | `/api/Roles/SetRole` | *(Admin)* Assign a role to a user |

---

### 📄 Pages

Each user can configure a default start page.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/Pages/GetPages` | List all available page definitions |
| `GET` | `/api/Pages/GetCurrentStartPage` | Get the current user's configured start page |
| `PUT` | `/api/Pages/SetStartPage` | Set the current user's start page |

---

### 📥 File export

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/File/GetExcelFile` | Export the user's portfolio to an Excel (.xlsx) file |

---

### ⚙️ Jobs

Manually trigger background jobs. Requires admin role.

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/Jobs/TriggerJob` | Trigger a named background job immediately (e.g. `RefreshSkinDynamicsJob`) |

---

### 🏥 Health checks

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/health-all` | JWT | All health checks combined (API, database, Steam API) |
| `GET /api/health-api` | — | API process liveness |
| `GET /api/health-db` | — | SQL Server connectivity |
| `GET /api/health-steam` | — | Steam Web API reachability |

---

### 📈 Metrics

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/metrics` | `Bearer <internalApiKey>` | Prometheus scrape endpoint — HTTP, HttpClient, and .NET runtime metrics |

---

## 🖥️ Admin panel

The Admin Panel (`localhost:8085`) is a server-rendered MVC application with SPA-style tab navigation — switching tabs and performing CRUD operations never triggers a full page reload.

**Tabs:**
- **Currencies** — add/edit/delete currencies, view exchange-rate history chart, users-per-currency chart
- **Games** — add/edit/delete games, view skins count and tracked items count per game
- **Users** — paginated user list with search, role assignment, user deletion
- **Jobs** — manually trigger `RefreshSkinDynamics`, `RefreshCurrencies`, `RefreshActiveGroupsDynamics`
- **Health** — live health status of API, database, and Steam connectivity

---

## 🔒 Security

### Rate limiting

Configured in `.config.yaml` under `rateLimit`. Defaults:

- Global: **20 req/s** per IP
- Health endpoints: **1000 req/s** (for load-balancer probes)
- Internal Docker subnet `172.20.0.0/24` is whitelisted

### Metrics endpoint

`/api/metrics` is protected by a static Bearer token (`internalApiKey`). Standard JWT validation is not used here because Prometheus does not support JWT. Configure the same key in `prometheus.yml` under `authorization.credentials`.

### Admin endpoints

Admin-only endpoints (`PostCurrency`, `SetRole`, `TriggerJob`, etc.) require the caller's JWT to carry the `Admin` role claim, enforced via policy-based authorization.

---

## 🛠️ Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10, C# 14 |
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + SQL Server |
| Auth | Steam OpenID → JWT Bearer |
| Background jobs | Quartz.NET 3 |
| Observability | OpenTelemetry → Prometheus → Grafana |
| Validation | FluentValidation 12 |
| API docs | Swashbuckle / Swagger |
| Rate limiting | AspNetCoreRateLimit |
| Containerisation | Docker Compose |
