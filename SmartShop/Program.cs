using SmartShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor view engine + controller routing
builder.Services.AddControllersWithViews();

// The simulation cursor must survive across HTTP requests, so the service is a singleton.
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var path = config["MockData:FilePath"] ?? "Data/mockdata.txt";
    return new MockDataService(path, config);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
