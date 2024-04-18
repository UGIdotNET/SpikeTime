using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace UGIdotNET.SpikeTime.FastEndpoints;

public class MyEndpoint : Endpoint<MyRequest, Results<Ok<MyResponse>, ProblemDetails>>
{
    public override void Configure()
    {
        Post("/api/user");
        AllowAnonymous();
        Version(1);
        //Throttle(
        //    hitLimit: 4,
        //    durationSeconds: 10);
        //ResponseCache(60);
    }

    public override async Task<Results<Ok<MyResponse>, ProblemDetails>> ExecuteAsync(MyRequest req, CancellationToken ct)
    {
        await Task.Delay(100);

        if (req.Age < 0) 
        {
            AddError(r => r.Age, "Age cannot be less than zero");
            return new ProblemDetails(ValidationFailures);
        }

        return TypedResults.Ok(new MyResponse
        {
            FullName = $"{req.FirstName} {req.LastName}",
            IsOver18 = req.Age > 18,
            Ticks = DateTime.UtcNow.Ticks
        });
    }

    //public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    //{
    //    await SendAsync(new()
    //    {
    //        FullName = $"{req.FirstName} {req.LastName}",
    //        IsOver18 = req.Age > 18
    //    }, statusCode: 201, cancellation: ct);
    //}
}
