using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using UGIdotNET.SpikeTime.SqlEvent;
using UGIdotNET.SpikeTime.SqlEvent.Components;
using UGIdotNET.SpikeTime.SqlEvent.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SqlServerEventsDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddSingleton(
    p => new BlobServiceClient(new(builder.Configuration.GetConnectionString("StorageAccount"))));

builder.Services.AddSingleton(p =>
{
    var blobContainerClient = p.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("spiketimesqles");
    var processor = new EventProcessorClient(
        blobContainerClient,
        EventHubConsumerClient.DefaultConsumerGroupName,
        builder.Configuration.GetConnectionString("EventHubs"),
        "sqlserverhub");

    return processor;
});

builder.Services.AddHostedService<EventListenerWorker>();

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
