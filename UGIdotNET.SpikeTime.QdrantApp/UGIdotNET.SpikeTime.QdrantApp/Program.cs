using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using UGIdotNET.SpikeTime.QdrantApp.Components;
using UGIdotNET.SpikeTime.QdrantApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddQdrantClient("qdrant");

builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<ulong, Episode>("spiketime_espisodes");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(
    sp =>
    {
        var azureOpenAIClient = new AzureOpenAIClient(
            new(builder.Configuration["AzureOpenAI:Endpoint"]!),
            new AzureKeyCredential(builder.Configuration["AzureOpenAI:Key"]!));

        return azureOpenAIClient
            .GetEmbeddingClient("text-embedding-ada-002")
            .AsIEmbeddingGenerator();
    });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
