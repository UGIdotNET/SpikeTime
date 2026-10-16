# Squad Decisions

## Active Decisions

### Lead — Proposta architettura piattaforma video (2026-10-16)

- Stack: .NET Aspire AppHost + ASP.NET Core Minimal API + Blazor Web App (SSR + interactive server) + EF Core (PostgreSQL in Aspire, SQLite ok in dev).
- Layout: `AppHost`, `ServiceDefaults`, `Api`, `Web`, `Domain/Data`, `Api.Tests`, `Web.Tests` (Playwright opzionale).
- Modello: Video(Id, Title, Description, Source, Url/BlobKey, CreatedAt), Rating(VideoId, Stars 1-5, VoterHash, CreatedAt), Comment(VideoId, Author, Text, Status Pending|Approved|Rejected, CreatedAt, ModeratedAt/By).
- Video: URL esterno/embed (YouTube/Vimeo) in MVP; upload su blob (Azure Blob/Azurite) rimandato.
- Auth admin: ASP.NET Core Identity + cookie (Web) / JWT (API), un solo ruolo Admin, seed da user-secrets; niente registrazione pubblica.
- Anti-abuso: 1 voto per video per cookie anonimo + hash IP (unique index), rate limiting (built-in middleware), commenti in stato Pending (pre-moderazione), validazione/lunghezza, HTML encoding, honeypot; captcha rimandato.
- API: GET /api/videos, GET /api/videos/{id}, POST /api/videos/{id}/ratings, GET/POST /api/videos/{id}/comments (solo Approved); admin: POST/PUT/DELETE /api/admin/videos, GET /api/admin/ratings, GET /api/admin/comments?status=, POST /api/admin/comments/{id}/approve|reject, DELETE.
- Fasi: 1 Backend (dominio, EF, API pubbliche, Aspire); 2 Backend (auth admin, moderazione, rate limit) + Frontend (elenco/dettaglio/voto/commenti); 3 Frontend (area admin); 4 Tester (unit/integration con Testcontainers, E2E Playwright, test abuso).
### Backend — Phase 1 + auth/moderazione (2026-10-16)

- Layout in `UGIdotNET.SpikeTime.Squad`: `apphost.cs` (AppHost file-based, creato da `aspire new`), `VideoHub.ServiceDefaults`, `VideoHub.Data`, `VideoHub.Api`, `VideoHub.slnx` (senza AppHost). net10.0.
- DB: PostgreSQL via Aspire anche in dev (richiesta utente); migrazioni in `VideoHub.Data`, applicate allo startup dell'Api.
- Auth: Identity core + Bearer token (`POST /api/auth/login`), ruolo `Admin`, admin seed da `Admin:Email`/`Admin:Password` (user-secrets). Niente registrazione, lockout 5 tentativi.
- Voti: unique index `(VideoId, VoterHash)` e `(VideoId, IpHash)`; voto non modificabile (409 se già votato). Trade-off: utenti dietro lo stesso NAT/IP condividono un solo voto per video.
- Identità votante: header `X-Voter-Id` (dal Web, che fa chiamate server-side) oppure cookie `vh_voter`; IP da `X-Forwarded-For` (proxy fidati = tutti: l'Api NON va esposta pubblicamente senza restringere). `Voting:HashSalt` obbligatorio fuori da Development.
- Commenti: Pending alla creazione, GET pubblico solo Approved, honeypot `website`, testo salvato raw (encoding in output a carico del frontend).
- Video: solo link https YouTube/Vimeo (allowlist host); `embedUrl` calcolato.
- Rate limit per IP: 10/min sui POST pubblici, 5/min sul login.

Questioni adiacenti (non toccate): warning NU1902 su MessagePack 2.5.302 transitivo del pacchetto AppHost Aspire; runtime non verificato end-to-end (nessun container runtime disponibile qui, solo build).
### Backend — verifica runtime (2026-10-16)

- Bug trovato e corretto: `RequireAuthorization("Admin")` indica una *policy*, non un ruolo → tutti gli endpoint `/api/admin/*` rispondevano 500. Registrata la policy `Admin` (`RequireRole("Admin")`) in `Program.cs`; ora 401 senza token.
- Verificato runtime con `aspire start` (Postgres + Api): migrazione, seed admin, login, CRUD video, voti, commenti/moderazione, rate limit (429 dopo 10 POST/min per IP, dopo 5 login/min).
- Non verificato: 403 con utente autenticato non-admin (nessun endpoint per creare utenti non-admin; test DB non eseguito).
### Frontend — VideoHub.Web fase 1 (2026-10-16)

- Blazor Web App **solo SSR statico** (nessun render mode interattivo): form POST con antiforgery + enhanced forms; nessun circuito, quindi il token non vive mai nel browser/JS.
- Auth admin: cookie `videohub_admin` (HttpOnly, cifrato da Data Protection) con claim Name/Role=Admin + claim con il bearer token; scadenza = expiresIn del token. `ApiContextHandler` (DelegatingHandler) aggiunge Bearer, `X-Voter-Id`, `X-Forwarded-For` (RemoteIpAddress) a ogni chiamata. Il ruolo Admin nel cookie è assunto (login API non restituisce ruoli); l'autorizzazione reale resta sull'Api.
- Voter id: cookie `vh_web_voter` (HttpOnly, 1 anno, 24 byte base64url = 32 char) creato da `VoterIdMiddleware`.
- Paginazione elenco: l'Api non restituisce il totale → "Successiva" mostrata se la pagina è piena (12 pubblico, 20 admin).
- Logout: `POST /logout` con validazione antiforgery manuale.
- Aspire: `web` con `WithReference(api)`, `WaitFor(api)`, `WithExternalHttpEndpoints()`; base address `https+http://api`.

Richieste/Questioni per Backend (non toccate):
- Messaggi di validazione 400 dell'Api sono in inglese (es. "Must be an https YouTube or Vimeo link."); mostrati così con prefisso italiano.
- Login non restituisce ruoli/scadenza refresh; nessun refresh token usato (sessione 8h poi nuovo login).
- Nessun totale/header di paginazione su `GET /api/videos`; `GET /api/admin/ratings` restituisce righe singole (aggregazione fatta nel Web).
- L'Api si fida di X-Forwarded-For da chiunque: non esporre pubblicamente.
### Tester — Phase 1: VideoHub.Api.Tests (2026-10-16)

- Nuovo progetto `VideoHub.Api.Tests` (xUnit 2, `WebApplicationFactory<Program>`), aggiunto a `VideoHub.slnx`. 126 test, nessun Docker.
- DB dei test: SQLite in-memory al posto di PostgreSQL. La factory rimuove le registrazioni `AppDbContext` di Aspire/Npgsql e usa le stesse migrazioni (PendingModelChangesWarning soppresso perché lo snapshot è Npgsql) + un `IModelCustomizer` che mappa `DateTimeOffset` a `long` (SQLite non sa ordinarlo). Limite noto: vincoli/tipi specifici Postgres non sono verificati; la correttezza delle query EF sì.
- Config via variabili d'ambiente impostate dalla factory (`ConnectionStrings__videohub` fittizia, `Admin__Email/Password` solo test). Nessun segreto reale.
- Ogni client di test ha un IP fittizio unico (`X-Forwarded-For`) per non condividere i bucket di rate limit; i test 429 usano un IP dedicato. Il test del rate limit sul login usa un account inesistente per non bloccare l'admin (lockout 5 tentativi).
- Utente non-admin creato via `UserManager` nella factory (l'Api non ha registrazione): verificato 403 su tutti gli endpoint admin.
- Modifica minima al codice di prodotto: `public partial class Program;` in `VideoHub.Api/Program.cs`.
- E2E Playwright NON realizzato: richiede AppHost + Postgres (Docker) e browser; fuori ambito senza fragilità. Da riprendere quando c'è un container runtime in CI.

### Backlog — cosa manca (2026-10-16)

#### Backend

- Messaggi di validazione 400 in inglese: localizzare in italiano.
- `GET /api/videos` senza totale: aggiungere `totalCount` per la paginazione.
- Login senza ruoli nella risposta (il Web assume Admin) e nessun refresh token (re-login dopo 8h).
- `X-Forwarded-For` accettato da qualsiasi proxy: restringere con KnownProxies/ForwardedHeaders prima di esporre pubblicamente.
- Voto unico anche per hash IP (utenti dietro lo stesso NAT condividono un voto): rivalutare.
- Warning NU1902 MessagePack 2.5.302 (dipendenza transitiva Aspire).
- Cambiare/rimuovere le credenziali admin di test dai user-secrets del progetto Api.

#### Tester

- Test E2E Playwright (anonimo vota e commenta, admin modera) non scritti.
- Test contro PostgreSQL reale (Testcontainers) per vincoli specifici Postgres, oggi solo SQLite in-memory.
- Test per VideoHub.Web assenti.

#### Prodotto/Fase 2

- Upload file video (oggi solo link https YouTube/Vimeo).
- Captcha anti-abuso rimandato.
- Eventuale provider di autenticazione esterno / più admin (oggi un solo admin con login locale).

#### Housekeeping

- Cartella `.agents` con skill Aspire creata da `aspire new`: decidere se tenerla/committarla.
- Nessun commit git ancora effettuato per il lavoro in UGIdotNET.SpikeTime.Squad e .squad.
- Aggiornare .squad/identity/now.md con il focus corrente e le fasi completate (Fase 1 backend, UI Blazor, test API).

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
