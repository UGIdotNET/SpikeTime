using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using UGIdotNET.SpikeTime.TelerikAI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelerikBlazor();

builder.Services.AddChatClient(sp =>
{
    var endpoint = builder.Configuration["AzureOpenAI:Endpoint"]!;
    var key = builder.Configuration["AzureOpenAI:Key"]!;

    var azureClient = new AzureOpenAIClient(
        new(endpoint),
        new Azure.AzureKeyCredential(key));

    return azureClient.GetChatClient("spiketime-fe-gpt-4.1")
        .AsIChatClient();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
