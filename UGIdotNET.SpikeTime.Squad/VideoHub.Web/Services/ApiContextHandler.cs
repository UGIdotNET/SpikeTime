using System.Net.Http.Headers;
using System.Security.Claims;

namespace VideoHub.Web.Services;

/// <summary>Forwards voter id, client IP and (for admins) the server-side bearer token to the API.</summary>
public class ApiContextHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        if (context is not null)
        {
            if (context.Items[VoterIdMiddleware.ItemKey] is string voterId)
            {
                request.Headers.TryAddWithoutValidation("X-Voter-Id", voterId);
            }

            if (context.Connection.RemoteIpAddress is { } ip)
            {
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip.ToString());
            }

            var token = context.User.FindFirstValue(AuthClaims.AccessToken);
            if (!string.IsNullOrEmpty(token) && request.Headers.Authorization is null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
