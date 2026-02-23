using frontend.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
// Register ApiService + HttpClient
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/app/DataProtectionKeys"));

builder.Services.AddHttpClient<ApiService>(client =>
    {
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";
        client.BaseAddress = new Uri(apiBaseUrl);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

/// <summary>
/// Fetches a product image from the backend and streams it to the browser.
/// Acts as a pass-through so image requests always go through the frontend server,
/// making the site work correctly from any machine on the network.
/// </summary>
app.MapGet("/api/proxy/image/{id:int}", async (int id, ApiService api) =>
{
    var response = await api.GetRawAsync($"/api/products/{id}/image");
    if (!response.IsSuccessStatusCode)
        return Results.NotFound();
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
    var stream = await response.Content.ReadAsStreamAsync();
    return Results.Stream(stream, contentType);
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.Run();