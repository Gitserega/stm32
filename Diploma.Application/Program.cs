using ApexCharts;
using Diploma.Application.Components;
using Diploma.Application.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped(sp => new HubConnectionBuilder()
    .WithUrl("http://localhost:5165/hubs/vibration")
    .WithAutomaticReconnect()
    .Build());
builder.Services.AddApexCharts();
builder.Services.AddMudServices();
builder.Services.AddHttpClient("AttendanceAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5165/");
});
builder.Services.AddSingleton<IApiService, ApiService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddMudBlazorDialog();
builder.Services.AddMudBlazorSnackbar();
var app = builder.Build();
app.UseStaticFiles();
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