using Nupack.Server.Web.Services;
using Nupack.Server.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register branding configuration
builder.Services.Configure<BrandingOptions>(
    builder.Configuration.GetSection(BrandingOptions.SectionName));

// Register HttpClient for NuGet API calls
builder.Services.AddHttpClient<INuGetApiService, NuGetApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("NuGetServer:BaseUrl") ?? "http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
