using Microsoft.EntityFrameworkCore;
using UGIdotNET.SpikeTime.VectorSql.Data;
using UGIdotNET.SpikeTime.VectorSql.Web;
using UGIdotNET.SpikeTime.VectorSql.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SpikeTimeDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("SpikeTimeVectorDb")));

builder.Services.AddScoped<EpisodeService>();
builder.Services.AddSingleton<IEmbeddingClient, AzureOpenAIEmbeddingClient>();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
