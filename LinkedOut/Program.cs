using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;  // DbContext sýnýfýnýzýn bulunduðu namespace

var builder = WebApplication.CreateBuilder(args);

// Baðlantý dizesini appsettings.json'dan al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDistributedMemoryCache(); // Session servisini ekleyelim kardeþþþþþþ 
builder.Services.AddSession(); // bu da session aþaðýda da orta katman var app de

builder.Services.AddDbContext<Context>(options =>
    options.UseSqlServer(connectionString));  // DbContext'i yapýlandýr

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

// Controller route ve static asset'leri düzenleyelim
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
