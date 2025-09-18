using SmartComponents.Inference;
using SmartComponents.Inference.OpenAI;
using UGIdotNET.SpikeTimeSmartComponents;
using UGIdotNET.SpikeTimeSmartComponents.Components;
using SmartComponents.LocalEmbeddings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSmartComponents()
    .WithInferenceBackend<OpenAIInferenceBackend>();

builder.Services.AddSingleton<LocalEmbedder>();

builder.Services.AddSingleton<SmartTextAreaInference, MySmartTextAreaInference>();

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

var embedder = app.Services.GetRequiredService<LocalEmbedder>();
var categories = embedder.EmbedRange([
    "Groceries", 
    "Utilities", 
    "Rent", 
    "Mortgage", 
    "Car Payment", 
    "Car Insurance", 
    "Health Insurance", 
    "Life Insurance", 
    "Home Insurance", 
    "Gas", 
    "Public Transportation", 
    "Dining Out", 
    "Entertainment", 
    "Travel", 
    "Clothing", 
    "Electronics", 
    "Home Improvement", 
    "Gifts", 
    "Charity", 
    "Education", 
    "Childcare", 
    "Pet Care", 
    "Other"]);

app.MapSmartComboBox(
    "api/combobox-items",
    request =>
    {
        return embedder.FindClosest(request.Query, categories);
    });

app.Run();
