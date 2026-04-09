using bento_blazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"] ?? "http://bento-gateway:5000/gateway";
builder.Services.AddHttpClient<BentoApiService>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
