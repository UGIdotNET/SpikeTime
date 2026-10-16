# VideoHub (backend)

Aspire AppHost + Minimal API + EF Core (PostgreSQL) for the video platform. No UI yet.

| Project | Role |
|---|---|
| `apphost.cs` | Aspire AppHost (PostgreSQL container + pgAdmin + Api) |
| `VideoHub.ServiceDefaults` | OpenTelemetry, health checks, resilience |
| `VideoHub.Data` | Domain, `AppDbContext` (Identity + EF Core), migrations |
| `VideoHub.Api` | Minimal API |

## Run

Requires .NET 10 SDK, Aspire CLI and a container runtime (Docker/Podman) for PostgreSQL.

```powershell
# one-time: admin credentials (stored in user-secrets, never committed)
dotnet user-secrets set "Admin:Email" "admin@example.com" --project VideoHub.Api
dotnet user-secrets set "Admin:Password" "<strong password: 6+ chars, upper, lower, digit, symbol>" --project VideoHub.Api

aspire run
```

On startup the Api applies migrations and creates the `Admin` role and the single admin user from `Admin:Email` / `Admin:Password`
(if missing, no admin is created). There is no public registration.
In non-Development environments set `Voting:HashSalt` (secret) too; it salts voter/IP hashes.

Build only: `dotnet build VideoHub.slnx` and `dotnet build apphost.cs`.

New migration: `dotnet ef migrations add <Name> -p VideoHub.Data -s VideoHub.Api`.

## Auth

`POST /api/auth/login` `{ "email", "password" }` returns `{ tokenType, accessToken, expiresIn, refreshToken }`.
Send `Authorization: Bearer <accessToken>` on `/api/admin/*` (role `Admin`). Token lifetime 8h. Lockout after 5 failures.

## Anonymous voter identity

One rating per video per voter, never modifiable (unique indexes on `(VideoId, VoterHash)` and `(VideoId, IpHash)`).
Voter id = `X-Voter-Id` header (a frontend-managed id, 16-64 chars `[A-Za-z0-9_-]`) or, if absent, the `vh_voter` cookie (set by the API).
IP = connection IP, honouring `X-Forwarded-For`. A frontend calling server-side must forward both headers.

## Endpoints

Public
- `GET /api/videos?page=1&pageSize=20` -> `[VideoSummary]`
- `GET /api/videos/{id}` -> `VideoSummary` | 404
- `GET /api/videos/{id}/ratings/mine` -> `{ "hasVoted": bool }`
- `POST /api/videos/{id}/ratings` `{ "stars": 1-5 }` -> 201 `{ averageRating, ratingCount }` | 400 | 404 | 409 (already voted) | 429
- `GET /api/videos/{id}/comments` -> `[{ id, author, text, createdAt }]` (Approved only)
- `POST /api/videos/{id}/comments` `{ author, text, website }` -> 202 `{ id, status: "Pending" }` | 400 | 404 | 429 (`website` is a honeypot, leave empty)

Admin (Bearer, role Admin)
- `POST /api/admin/videos`, `PUT /api/admin/videos/{id}` body `{ title, description, url }` -> `VideoSummary`; `DELETE /api/admin/videos/{id}` -> 204
- `GET /api/admin/ratings?videoId=` -> `[{ id, videoId, videoTitle, stars, createdAt }]`
- `GET /api/admin/comments?status=Pending|Approved|Rejected` (default: all) -> `[AdminComment]`
- `POST /api/admin/comments/{id}/approve|reject` -> `AdminComment`; `DELETE /api/admin/comments/{id}` -> 204

`VideoSummary`: `{ id, title, description, url, embedUrl, createdAt, averageRating, ratingCount }`
(`embedUrl` is null if the link can't be turned into an embed).
`AdminComment`: `{ id, videoId, videoTitle, author, text, status, createdAt, moderatedAt, moderatedBy }`.

Limits: title 200, description 2000, url 2000 (https YouTube/Vimeo only), author 80, comment 1000.
Rate limits (per IP): public POSTs 10/min, login 5/min. Validation errors are `400` ProblemDetails with an `errors` map.
Comment text is stored raw: the frontend must HTML-encode on output (Blazor does by default).
