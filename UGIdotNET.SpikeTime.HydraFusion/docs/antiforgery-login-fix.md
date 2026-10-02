# Correzione antiforgery del portale amministrativo

> Nota: il form di login statico descritto in questo documento è stato successivamente
> sostituito dal flusso OpenID Connect di Keycloak. La protezione antiforgery resta attiva
> sul form di logout locale.

## Sintomo

L'invio del form di accesso del portale Blazor verso `POST /login` restituiva un errore
HTTP 400. Nei log strutturati Aspire del servizio `adminportal` era presente la seguente
eccezione:

```text
Microsoft.AspNetCore.Http.BadHttpRequestException:
Invalid anti-forgery token found when reading parameter "string username"
from the request body as form.

Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException:
The required antiforgery request token was not provided in either form field
"__RequestVerificationToken" or header value "RequestVerificationToken".
```

## Causa

Il portale configura correttamente il middleware antiforgery:

```csharp
app.UseAntiforgery();
```

Gli endpoint Minimal API `/login` e `/logout` ricevono dati inviati come form. ASP.NET Core
applica quindi la validazione antiforgery, ma i due form HTML in `Home.razor` non
includevano il campo nascosto `__RequestVerificationToken`.

Il browser inviava il cookie antiforgery generato dal server, ma non il corrispondente
request token. La coppia richiesta dalla validazione risultava incompleta e la richiesta
veniva rifiutata prima dell'esecuzione dell'endpoint.

## Piano di risoluzione

1. Confermare l'errore tramite i log strutturati Aspire.
2. Mantenere attiva la protezione antiforgery.
3. Inserire il componente Blazor `AntiforgeryToken` nei form di login e logout.
4. Ricostruire soltanto la risorsa Aspire `adminportal`.
5. Verificare con richieste HTTP reali l'intero flusso login/logout.

## Soluzione applicata

In `VideoCatalog.AdminPortal/Components/Pages/Home.razor` è stato aggiunto
`<AntiforgeryToken />` a entrambi i form:

```razor
<form method="post" action="/login">
    <AntiforgeryToken />
    ...
</form>

<form method="post" action="/logout">
    <AntiforgeryToken />
    ...
</form>
```

Il componente genera il campo nascosto atteso dal middleware:

```html
<input name="__RequestVerificationToken" type="hidden" value="..." />
```

Non è stato usato `DisableAntiforgery()`: disabilitare la validazione avrebbe eliminato
l'errore, ma avrebbe anche rimosso la protezione CSRF dagli endpoint che creano e
terminano la sessione amministrativa.

## Verifica

La risorsa è stata ricostruita con:

```powershell
aspire resource adminportal rebuild --non-interactive
aspire wait adminportal --non-interactive
```

È stato poi eseguito un flusso HTTP completo:

1. `GET /` con conservazione dei cookie.
2. Estrazione di `__RequestVerificationToken` dal form.
3. `POST /login` con token e credenziali di sviluppo.
4. Verifica della pagina amministrativa e del cookie `videocatalog-admin`.
5. Estrazione del token dal form autenticato.
6. `POST /logout` con token.
7. Verifica del ritorno alla pagina di accesso e della rimozione del cookie.

Risultato:

```text
LoginSucceeded: true
LogoutSucceeded: true
AuthCookieRemoved: true
```

La build del portale amministrativo è terminata senza avvisi né errori.
