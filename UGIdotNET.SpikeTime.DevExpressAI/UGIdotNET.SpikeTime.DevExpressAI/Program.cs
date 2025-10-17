using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;
using UGIdotNET.SpikeTime.DevExpressAI;
using UGIdotNET.SpikeTime.DevExpressAI.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var modelName = "spiketime-fe-gpt-4.1";
var azureOpenAIClient = new AzureOpenAIClient(
    new(builder.Configuration["AzureOpenAI:Endpoint"]!),
    new System.ClientModel.ApiKeyCredential(builder.Configuration["AzureOpenAI:Key"]!));

builder.Services
    .AddChatClient(azureOpenAIClient.GetChatClient(modelName).AsIChatClient());

builder.Services.AddEmbeddingGenerator(
    sp => azureOpenAIClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator());

builder.Services.AddSingleton<SmartFilterProvider>();

builder.Services.AddDevExpressBlazor();
builder.Services.AddDevExpressAI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
