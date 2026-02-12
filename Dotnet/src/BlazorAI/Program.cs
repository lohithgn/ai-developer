using BlazorAI.Components;
using BlazorAI.Components.Models;
using BlazorAI.Queue;
using Microsoft.FluentUI.AspNetCore.Components;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.AddFilter("Microsoft", LogLevel.Error);
    loggingBuilder.AddFilter("System", LogLevel.Error);
    loggingBuilder.AddFilter("Microsoft.Identity", LogLevel.Error);
});

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();
builder.Services.AddFluentUIComponents();

builder.Services.AddScoped<AppState>();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(sp => new BackgroundTaskQueue(100));
builder.Services.AddHostedService<CoordinatorQueueService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAntiforgery();
app.MapBlazorHub().WithOrder(-1);

app.MapDefaultEndpoints();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
