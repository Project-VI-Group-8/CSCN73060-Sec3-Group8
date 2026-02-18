using frontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession();
// Register ApiService + HttpClient
builder.Services.AddHttpClient<ApiService>(client =>
{
    //client.BaseAddress = new Uri("http://localhost:3000");
    client.BaseAddress = new Uri("http://localhost:8080");

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

app.UseAuthorization();
app.UseSession();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.UseStaticFiles();
app.Run();
