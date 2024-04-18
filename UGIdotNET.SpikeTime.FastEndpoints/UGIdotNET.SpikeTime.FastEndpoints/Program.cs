using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.MaxEndpointVersion = 1;
        o.DocumentSettings = s =>
        {
            s.DocumentName = "Release 1.0";
            s.Title = "my api";
            s.Version = "v1.0";
        };
    })
   .SwaggerDocument(o =>
   {
       o.MaxEndpointVersion = 2;
       o.DocumentSettings = s =>
       {
           s.DocumentName = "Release 2.0";
           s.Title = "my api";
           s.Version = "v2.0";
       };
   });

var app = builder.Build();

app
    .UseFastEndpoints(c =>
    {
        c.Versioning.Prefix = "v";
        c.Versioning.DefaultVersion = 1;
        c.Versioning.PrependToRoute = true;
    })
    .UseSwaggerGen();

app.MapGet("/", () => "Hello World!");

app.Run();
