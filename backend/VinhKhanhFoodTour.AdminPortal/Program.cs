using VinhKhanhFoodTour.AdminPortal.Components;
using VinhKhanhFoodTour.AdminPortal.Services.Auth;
using VinhKhanhFoodTour.AdminPortal.Services.Http;
using VinhKhanhFoodTour.AdminPortal.Services.Owner;
using VinhKhanhFoodTour.AdminPortal.Services.Admin;
using VinhKhanhFoodTour.AdminPortal.Services.Common;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

// Register LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register HTTP Client and Services
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<IPoiService, PoiService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<ITranslationService, TranslationService>();

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

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
