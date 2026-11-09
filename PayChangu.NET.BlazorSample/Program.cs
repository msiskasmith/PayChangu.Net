using PayChangu.NET.BlazorSample.Components;
using PayChangu.NET.Extensions;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

var secretKey = builder.Configuration["PayChangu:SecretKey"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "PayChangu:SecretKey is missing. Configure it using .NET User Secrets. " +
        "Run: dotnet user-secrets set \"PayChangu:SecretKey\" \"<your-secret-key>\"");
}

builder.Services.AddPayChangu(options =>
{
    options.SecretKey = secretKey;
    options.WebhookSecret = builder.Configuration["PayChangu:WebhookSecret"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
