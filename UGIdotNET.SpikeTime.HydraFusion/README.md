# Video Catalog

Applicazione distribuita orchestrata con .NET Aspire:

- **Public Portal**: Astro con markup SEO del catalogo e votazione anonima da 1 a 5.
- **Public API**: ASP.NET Core Minimal API per lettura contenuti e registrazione voti.
- **Admin Portal**: Blazor Interactive Server autenticato tramite Keycloak e OpenID Connect.
- **Admin API**: ASP.NET Core Minimal API protetta da bearer token Keycloak e ruolo `video-admin`.
- **Identity Provider**: Keycloak orchestrato da Aspire con realm importato automaticamente.
- **Database**: PostgreSQL gestito come risorsa Aspire.

## Configurazione locale

Sono richiesti .NET 10, Docker e Aspire CLI.

I secret di sviluppo non sono versionati. Configurarli negli user secrets dell'AppHost:

```powershell
dotnet user-secrets set "Parameters:keycloak-admin-password" "<password-keycloak>" --project VideoCatalog.AppHost\VideoCatalog.AppHost.csproj
dotnet user-secrets set "Parameters:keycloak-client-secret" "<client-secret-admin-portal>" --project VideoCatalog.AppHost\VideoCatalog.AppHost.csproj
dotnet user-secrets set "Parameters:keycloak-dev-user-password" "<password-utente-admin>" --project VideoCatalog.AppHost\VideoCatalog.AppHost.csproj
```

L'utente applicativo importato nel realm è `admin`; la sua password corrisponde al
parametro `keycloak-dev-user-password`.

## Avvio

```powershell
cd VideoCatalog.AppHost
aspire run
```

Aspire espone gli endpoint nel dashboard. In sviluppo Keycloak usa l'endpoint stabile
`https://localhost:8080`, necessario per callback e cookie OIDC consistenti.

Il realm `video-catalog` viene importato da
`VideoCatalog.AppHost\Keycloak\video-catalog-realm.json` e contiene:

- client confidenziale `admin-portal`;
- resource server `admin-api`;
- ruolo `video-admin`;
- utente locale `admin` con ruolo amministrativo.

Il portale usa Authorization Code con PKCE e mantiene la sessione in un cookie HTTP-only.
L'access token resta sul server Blazor e viene inoltrato all'Admin API.

## API

| Metodo | Endpoint | Accesso |
|---|---|---|
| `GET` | `/api/public/videos` | Pubblico |
| `POST` | `/api/public/videos/{id}/ratings` | Pubblico |
| `GET` | `/api/admin/videos` | Keycloak + ruolo `video-admin` |
| `POST` | `/api/admin/videos` | Keycloak + ruolo `video-admin` |

Il body della votazione è `{ "rating": 1..5 }`. Il body di creazione contiene `title`,
`description` e `url`; la piattaforma viene riconosciuta dal link.

## Pacchetti Keycloak

L'integrazione Keycloak di Aspire 13.4.6 è distribuita come preview. AppHost, Admin Portal
e Admin API usano la versione `13.4.6-preview.1.26319.6`, allineata alla versione Aspire
del repository.
