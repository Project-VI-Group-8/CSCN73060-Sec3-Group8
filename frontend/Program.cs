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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.Run();