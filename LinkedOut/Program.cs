using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;  // DbContext s�n�f�n�z�n bulundu�u namespace

var builder = WebApplication.CreateBuilder(args);

// Ba�lant� dizesini appsettings.json'dan al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDistributedMemoryCache(); // Session servisini ekleyelim karde������ 
builder.Services.AddSession(); // bu da session a�a��da da orta katman var app de

builder.Services.AddDbContext<Context>(options =>
    options.UseSqlServer(connectionString));  // DbContext'i yap�land�r

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession(); // session middleware
app.UseAuthorization();
app.MapStaticAssets();

// Controller route ve static asset'leri d�zenleyelim
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
