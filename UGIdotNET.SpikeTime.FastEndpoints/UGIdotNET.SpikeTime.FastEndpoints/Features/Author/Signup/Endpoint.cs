using FastEndpoints;
using Microsoft.AspNetCore.Authorization;

namespace Author.Signup;

[HttpPost("/author/signup")]
[AllowAnonymous]
internal sealed class Endpoint : Endpoint<Request, Response, Mapper>
{
    private ILogger<Endpoint> _logger;

    public Endpoint(ILogger<Endpoint> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task HandleAsync(Request r, CancellationToken c)
    {
        await SendAsync(new Response()
        {
            Message = $"hello {r.FirstName} {r.LastName}! your request has been received!"
        });
    }

    public override void OnBeforeValidate(Request req)
    {
        base.OnBeforeValidate(req);
        _logger.LogInformation("Validating request for user {FirstName} {LastName}",
            req.FirstName, req.LastName);
    }

    public override void OnAfterValidate(Request req)
    {
        base.OnAfterValidate(req);
        _logger.LogInformation(
            "Validated request for user {FirstName} {LastName}",
            req.FirstName, req.LastName);
    }
}