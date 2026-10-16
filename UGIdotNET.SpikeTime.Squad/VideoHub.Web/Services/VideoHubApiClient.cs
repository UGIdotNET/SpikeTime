using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Web.Models;

namespace VideoHub.Web.Services;

public class VideoHubApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<List<VideoSummary>>> GetVideosAsync(int page, int pageSize, CancellationToken ct = default) =>
        SendAsync<List<VideoSummary>>(HttpMethod.Get, $"api/videos?page={page}&pageSize={pageSize}", null, ct);

    public Task<ApiResult<VideoSummary>> GetVideoAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<VideoSummary>(HttpMethod.Get, $"api/videos/{id}", null, ct);

    public Task<ApiResult<MyRating>> GetMyRatingAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<MyRating>(HttpMethod.Get, $"api/videos/{id}/ratings/mine", null, ct);

    public Task<ApiResult<RatingSummary>> RateAsync(Guid id, int stars, CancellationToken ct = default) =>
        SendAsync<RatingSummary>(HttpMethod.Post, $"api/videos/{id}/ratings", new { stars }, ct);

    public Task<ApiResult<List<PublicComment>>> GetCommentsAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<List<PublicComment>>(HttpMethod.Get, $"api/videos/{id}/comments", null, ct);

    public Task<ApiResult<object>> PostCommentAsync(Guid id, CommentForm form, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Post, $"api/videos/{id}/comments",
            new { author = form.Author, text = form.Text, website = form.Website }, ct);

    public Task<ApiResult<LoginResponse>> LoginAsync(LoginForm form, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new { email = form.Email, password = form.Password }, ct, isLogin: true);

    public Task<ApiResult<VideoSummary>> CreateVideoAsync(VideoForm form, CancellationToken ct = default) =>
        SendAsync<VideoSummary>(HttpMethod.Post, "api/admin/videos", form, ct);

    public Task<ApiResult<VideoSummary>> UpdateVideoAsync(Guid id, VideoForm form, CancellationToken ct = default) =>
        SendAsync<VideoSummary>(HttpMethod.Put, $"api/admin/videos/{id}", form, ct);

    public Task<ApiResult<object>> DeleteVideoAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/admin/videos/{id}", null, ct);

    public Task<ApiResult<List<AdminRating>>> GetAdminRatingsAsync(Guid? videoId, CancellationToken ct = default) =>
        SendAsync<List<AdminRating>>(HttpMethod.Get, videoId is { } v ? $"api/admin/ratings?videoId={v}" : "api/admin/ratings", null, ct);

    public Task<ApiResult<List<AdminComment>>> GetAdminCommentsAsync(string? status, CancellationToken ct = default) =>
        SendAsync<List<AdminComment>>(HttpMethod.Get,
            string.IsNullOrEmpty(status) ? "api/admin/comments" : $"api/admin/comments?status={Uri.EscapeDataString(status)}", null, ct);

    public Task<ApiResult<AdminComment>> ModerateCommentAsync(Guid id, bool approve, CancellationToken ct = default) =>
        SendAsync<AdminComment>(HttpMethod.Post, $"api/admin/comments/{id}/{(approve ? "approve" : "reject")}", null, ct);

    public Task<ApiResult<object>> DeleteCommentAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/admin/comments/{id}", null, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken ct, bool isLogin = false)
    {
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: Json);
            }

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Fail(response.StatusCode, await DescribeAsync(response, isLogin, ct));
            }

            var hasBody = response.Content.Headers.ContentLength is not 0 && response.StatusCode != HttpStatusCode.NoContent;
            var value = hasBody && typeof(T) != typeof(object) ? await response.Content.ReadFromJsonAsync<T>(Json, ct) : default;
            return ApiResult<T>.Ok(value, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return ApiResult<T>.Fail(null, "Servizio momentaneamente non raggiungibile. Riprova tra poco.");
        }
    }

    private static async Task<string> DescribeAsync(HttpResponseMessage response, bool isLogin, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                return await ReadValidationAsync(response, ct) ?? "Richiesta non valida. Controlla i dati inseriti.";
            case HttpStatusCode.Unauthorized:
                return isLogin
                    ? "Credenziali non valide oppure account temporaneamente bloccato."
                    : "Sessione scaduta. Effettua di nuovo l'accesso.";
            case HttpStatusCode.Forbidden:
                return "Non hai i permessi per eseguire questa operazione.";
            case HttpStatusCode.NotFound:
                return "Elemento non trovato.";
            case HttpStatusCode.Conflict:
                return "Hai già votato questo video: il voto non può essere modificato.";
            case HttpStatusCode.TooManyRequests:
                return "Troppe richieste. Attendi un minuto e riprova.";
            default:
                return "Si è verificato un errore imprevisto. Riprova più tardi.";
        }
    }

    private static async Task<string?> ReadValidationAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(e => e.GetString()))
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                var text = string.Join(" ", messages);
                return text.Length > 0 ? "Dati non validi: " + text : null;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
