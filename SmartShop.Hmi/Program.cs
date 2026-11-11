using SmartShop.Hmi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<DashboardStateBridge>();

var app = builder.Build();
app.UseStaticFiles();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
app.Run();
