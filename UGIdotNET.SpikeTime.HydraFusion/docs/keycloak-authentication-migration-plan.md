# Piano di migrazione dell'autenticazione a Keycloak

> Stato: implementato e verificato il 2 ottobre 2026.

## Obiettivo

Sostituire l'autenticazione attuale del portale amministrativo, basata su:

- credenziali statiche `AdminUser` / `AdminPassword`;
- cookie locale emesso direttamente dal portale Blazor;
- token JWT firmati con `AdminJwtSecret` per chiamare l'Admin API;

con un'autenticazione centralizzata tramite **Keycloak** e protocollo **OpenID Connect /
OAuth 2.0**.

Il portale pubblico e le API pubbliche resteranno anonimi. La protezione riguarderà:

1. il portale amministrativo Blazor;
2. le API amministrative Minimal API.

## Stato iniziale rilevato

### AppHost

`VideoCatalog.AppHost/AppHost.cs` configura PostgreSQL, Public API, Admin API, Admin Portal
e Public Portal. Attualmente passa al portale amministrativo:

```text
AdminJwtSecret
AdminUser
AdminPassword
```

La password di sviluppo è presente in chiaro nell'AppHost. Questo valore dovrà essere
rimosso dalla configurazione applicativa e sostituito con credenziali gestite da Keycloak
e, per il bootstrap locale, da parametri o secret non versionati.

### Admin Portal

`VideoCatalog.AdminPortal/Program.cs` usa:

- `CookieAuthenticationDefaults.AuthenticationScheme`;
- endpoint locali `POST /login` e `POST /logout`;
- `AdminUser` e `AdminPassword`;
- `VideoAdminClient`, che crea localmente JWT firmati con `AdminJwtSecret`.

Il cookie locale potrà essere mantenuto come sessione applicativa, ma dovrà essere emesso
dal middleware OIDC dopo il login su Keycloak, non da credenziali verificate dal portale.

### Admin API

`VideoCatalog.AdminApi/Program.cs` usa JWT Bearer con una chiave simmetrica condivisa.
La validazione dovrà passare a JWT emessi da Keycloak tramite:

- authority/issuer del realm;
- audience del client API;
- HTTPS metadata in ambienti non locali;
- eventuale ruolo o scope amministrativo.

## Architettura target

```text
Browser
  |
  | OpenID Connect Authorization Code + PKCE
  v
Admin Portal (Blazor)
  |
  | Cookie applicativo dopo callback OIDC
  | access token ottenuto da Keycloak
  v
Admin API (Minimal API)
  |
  v
PostgreSQL

Keycloak
  └── Realm video-catalog
      ├── Client admin-portal
      ├── Client admin-api
      └── Role video-admin
```

Il portale amministrativo sarà un client confidenziale OIDC con redirect URI locale
configurata per l'endpoint HTTPS Aspire. L'Admin API sarà una resource server che valida
localmente i token tramite la configurazione OpenID Connect del realm.

## Piano di lavoro

### 1. Verificare l'integrazione Aspire disponibile

Prima di modificare l'AppHost:

1. cercare l'integrazione Keycloak disponibile nella versione Aspire in uso;
2. usare l'integrazione ufficiale se supporta container, endpoint e configurazione;
3. se non disponibile o non sufficiente, usare un container Keycloak esplicito nel modello
   Aspire, senza gestire Keycloak con processi esterni;
4. consultare la documentazione/API Aspire prima di modificare il modello AppHost.

L'integrazione dovrà esporre almeno:

- endpoint HTTP/HTTPS di Keycloak;
- variabili di ambiente per realm, issuer e client;
- persistenza opzionale dei dati Keycloak per lo sviluppo locale;
- dipendenza `WaitFor` dal servizio Keycloak per i servizi che lo usano.

### 2. Configurare realm, client e ruolo

Creare una configurazione ripetibile per il realm `video-catalog`, preferibilmente tramite
realm import versionato o bootstrap idempotente:

- realm: `video-catalog`;
- client OIDC: `admin-portal`;
- client/resource server: `admin-api`;
- ruolo realm o client: `video-admin`;
- utente amministrativo locale solo per sviluppo;
- redirect URI e post-logout redirect URI limitati agli endpoint locali necessari;
- web origins limitate all'origine del portale amministrativo.

Le password iniziali di Keycloak e dell'utente amministrativo non dovranno essere inserite
nel codice sorgente o nel realm import in chiaro. Per lo sviluppo potranno essere gestite
tramite user secrets, variabili d'ambiente o parametri Aspire.

### 3. Migrare l'Admin Portal a OpenID Connect

In `VideoCatalog.AdminPortal`:

1. aggiungere il pacchetto ASP.NET Core OpenID Connect compatibile con .NET 10;
2. configurare lo schema cookie come sessione locale;
3. configurare lo schema OpenID Connect con authority, client ID, client secret,
   response type `code`, PKCE e scope `openid profile`;
4. sostituire il form login locale con un link o endpoint `ChallengeAsync`;
5. configurare il logout OIDC con ritorno alla pagina principale;
6. mantenere `UseAuthentication`, `UseAuthorization` e antiforgery;
7. eliminare la verifica diretta di `AdminUser` / `AdminPassword`;
8. rimuovere l'emissione di cookie locale basata su `SignInAsync` con credenziali statiche.

Il flusso di login dovrà essere:

```text
GET /login
  -> Challenge OIDC
  -> Keycloak login
  -> callback OIDC
  -> cookie applicativo
  -> pagina amministrativa
```

### 4. Migrare l'Admin API a JWT Keycloak

In `VideoCatalog.AdminApi`:

1. sostituire la chiave simmetrica con `AddJwtBearer` configurato tramite authority;
2. validare issuer, audience e lifetime;
3. configurare `RequireHttpsMetadata` secondo ambiente;
4. aggiungere una policy `VideoAdmin`;
5. applicare la policy agli endpoint amministrativi;
6. mappare il ruolo Keycloak nelle role claims, se necessario;
7. rimuovere `AdminJwtSecret` e `AdminTokenFactory`.

Gli endpoint `GET /api/admin/videos` e `POST /api/admin/videos` dovranno accettare solo
token Keycloak con il ruolo o scope amministrativo previsto.

### 5. Aggiornare il client del portale

`VideoAdminClient` non dovrà più creare JWT localmente. Dovrà usare l'access token
associato alla sessione OIDC, preferibilmente tramite:

- token salvato nel ticket di autenticazione con `SaveTokens = true`;
- `IHttpContextAccessor` o un handler HTTP dedicato;
- inoltro del bearer token verso l'Admin API.

Il token non dovrà essere esposto a JavaScript o memorizzato in local storage del browser.

### 6. Aggiornare AppHost e configurazione

In `VideoCatalog.AppHost/AppHost.cs`:

- aggiungere Keycloak come risorsa Aspire;
- collegare Admin Portal e Admin API al realm/issuer;
- passare client ID e endpoint tramite service discovery o configurazione;
- passare i secret solo tramite parametri/secret Aspire;
- rimuovere `AdminJwtSecret`, `AdminUser` e `AdminPassword`;
- aggiungere `WaitFor` per assicurare l'avvio di Keycloak prima dei consumer.

I nomi delle API Aspire saranno verificati con la documentazione della versione 13.4.6
presente nel repository; non verranno introdotte chiamate basate su API ipotizzate.

### 7. Aggiornare dipendenze e documentazione

Aggiornare:

- file `.csproj` dei progetti interessati;
- `README.md` con avvio, realm, credenziali di sviluppo e URL di Keycloak;
- eventuali `appsettings.Development.json` con soli valori non sensibili;
- documentazione del portale admin;
- test e istruzioni per il primo login.

Non verranno aggiunti secret reali al repository.

## Strategia di compatibilità

La migrazione sarà eseguita come modifica coordinata dei servizi, non mantenendo due
meccanismi di autenticazione in produzione. Durante lo sviluppo:

1. Keycloak viene avviato da Aspire;
2. il realm viene inizializzato automaticamente;
3. il portale admin redirige a Keycloak;
4. l'Admin API rifiuta i vecchi JWT firmati con `AdminJwtSecret`.

In questo modo si evita che il vecchio account statico rimanga un percorso di accesso
alternativo.

## Verifiche previste

### Build

```powershell
dotnet build VideoCatalog.slnx --nologo
```

### Avvio Aspire

```powershell
cd VideoCatalog.AppHost
aspire run --non-interactive
aspire wait keycloak --non-interactive
aspire wait adminapi --non-interactive
aspire wait adminportal --non-interactive
```

### Test funzionali

1. accesso al portale admin senza sessione: redirect a Keycloak;
2. login con utente Keycloak: ritorno al portale;
3. caricamento dei contenuti tramite Admin API;
4. creazione di un contenuto tramite Admin API;
5. token senza ruolo `video-admin`: risposta `403`;
6. token emesso da issuer errato: risposta `401`;
7. logout: invalidazione della sessione e ritorno a Keycloak;
8. accesso al Public Portal e votazione anonima invariati;
9. assenza di `AdminJwtSecret`, `AdminUser` e `AdminPassword` nella configurazione runtime.

### Sicurezza

- nessuna password o client secret versionato;
- redirect URI non permissive;
- validazione issuer e audience attiva;
- policy di ruolo applicata alle API;
- antiforgery mantenuto sui form locali eventualmente presenti;
- access token non esposto al browser tramite local storage.

## Rischi e decisioni da confermare durante l'implementazione

| Area | Rischio | Decisione pianificata |
|---|---|---|
| Persistenza Keycloak | Realm perso al riavvio | Usare volume Aspire in sviluppo |
| Bootstrap realm | Configurazione non idempotente | Realm import o bootstrap ripetibile |
| Ruoli | Formato claim variabile | Configurare mapping esplicito e testarlo |
| Redirect URI | URL Aspire dinamico | Usare endpoint stabile configurato oppure aggiornare il realm in sviluppo |
| Secret locale | Secret accidentali nel repository | Parametri Aspire/user secrets, nessun valore hardcoded |
| Logout | Sessione applicativa ancora valida | Logout coordinato cookie + OIDC |

## Criterio di completamento

La migrazione sarà considerata completata quando:

- l'Admin Portal autentica esclusivamente tramite Keycloak;
- l'Admin API accetta esclusivamente token emessi dal realm configurato;
- il ruolo `video-admin` protegge gli endpoint amministrativi;
- il flusso login/logout è verificato end-to-end;
- l'applicazione continua a essere orchestrata da Aspire;
- il README e questa documentazione riflettono la configurazione effettiva;
- non restano credenziali statiche o secret JWT nel codice.

## Ordine di esecuzione

Le attività verranno eseguite in questo ordine per ridurre i periodi in cui i servizi
usano configurazioni incompatibili:

1. Verificare e scegliere l'integrazione Keycloak compatibile con Aspire 13.4.6.
2. Aggiungere Keycloak all'AppHost con persistenza locale e configurazione del realm.
3. Configurare client, ruolo `video-admin` e utente di sviluppo senza versionare secret.
4. Migrare l'Admin API alla validazione dei bearer token Keycloak.
5. Migrare l'Admin Portal al challenge OpenID Connect e al cookie applicativo.
6. Aggiornare `VideoAdminClient` per inoltrare l'access token OIDC, eliminando la firma JWT locale.
7. Rimuovere `AdminJwtSecret`, `AdminUser`, `AdminPassword` e gli endpoint di login statici.
8. Ricostruire e avviare l'AppHost con Aspire, quindi eseguire i test funzionali e di sicurezza.
9. Aggiornare README e configurazione di sviluppo con gli endpoint e il primo-login flow.

## Fuori ambito

- autenticazione degli utenti anonimi del portale pubblico;
- modifica delle API pubbliche o del sistema di votazione;
- gestione multi-tenant o federazione con provider esterni;
- provisioning di Keycloak per ambienti cloud di produzione;
- migrazione di utenti esistenti, poiché l'applicazione attuale usa una singola utenza
  statica di sviluppo.

## Implementazione completata

### Aspire e Keycloak

- aggiunto `Aspire.Hosting.Keycloak` `13.4.6-preview.1.26319.6`;
- aggiunta la risorsa `keycloak` sulla porta stabile 8080;
- aggiunti volume persistente e realm import;
- spostati password amministrativa, client secret e password utente negli user secrets
  dell'AppHost;
- collegati `adminportal` e `adminapi` tramite `WithReference` e `WaitFor`;
- rimossi `AdminJwtSecret`, `AdminUser` e `AdminPassword`.

Il realm versionato è in
`VideoCatalog.AppHost/Keycloak/video-catalog-realm.json`. I placeholder dei secret sono
risolti dalle variabili d'ambiente passate da Aspire durante l'import.

### Admin Portal

- sostituito il login statico con challenge OpenID Connect;
- configurato Authorization Code con PKCE;
- mantenuto un cookie applicativo HTTP-only;
- salvati i token nel ticket di autenticazione server-side;
- inoltrato l'access token all'Admin API tramite `VideoAdminClient`;
- configurato logout coordinato cookie + OIDC;
- mantenuta la validazione antiforgery sul form di logout.

### Admin API

- sostituita la chiave JWT simmetrica con `AddKeycloakJwtBearer`;
- configurati realm `video-catalog` e audience `admin-api`;
- aggiunta la policy `VideoAdmin`;
- normalizzati i ruoli Keycloak dai claim `roles` e `realm_access`;
- applicato `RequireRole("video-admin")` a entrambi gli endpoint amministrativi.

## Verifica eseguita

- build della soluzione senza avvisi o errori;
- risorse `keycloak`, `adminapi`, `adminportal` e `publicportal` healthy;
- discovery OpenID Connect del realm disponibile;
- chiamata anonima all'Admin API rifiutata con `401`;
- redirect dal portale alla pagina di login Keycloak;
- login Authorization Code + PKCE completato;
- cookie applicativo emesso;
- access token inoltrato all'Admin API;
- catalogo amministrativo caricato con ruolo `video-admin`;
- logout OIDC completato e cookie applicativo rimosso;
- portale pubblico rimasto operativo.

## Nota sull'integrazione

L'integrazione Keycloak è ancora distribuita come pacchetto preview. È stata usata la
versione `13.4.6-preview.1.26319.6`, corrispondente alla versione Aspire 13.4.6 del
progetto, per evitare disallineamenti con le API dell'AppHost.
