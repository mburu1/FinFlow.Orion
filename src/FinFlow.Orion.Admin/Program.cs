using FinFlow.Orion.Admin;
using FinFlow.Orion.Admin;
using FinFlow.Orion.Admin.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── Root Components ────────────────────────────────────────────────────────
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── HTTP Client ────────────────────────────────────────────────────────────
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5000";

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ── MudBlazor ──────────────────────────────────────────────────────────────
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
});

// ── Logging ────────────────────────────────────────────────────────────────
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();
