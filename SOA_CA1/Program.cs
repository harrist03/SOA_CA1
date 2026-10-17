using SOA_CA1.Components;
using SOA_CA1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IMarketDataService, MarketDataService>(client =>
    client.BaseAddress = new Uri("https://finnhub.io/api/v1/"));
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
    client.BaseAddress = new Uri("https://api.frankfurter.dev/v2/"));
builder.Services.AddSingleton<IHoldingsRepository, HoldingsRepository>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

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