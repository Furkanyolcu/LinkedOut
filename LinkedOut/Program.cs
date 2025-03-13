using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;  // DbContext s�n�f�n�z�n bulundu�u namespace

var builder = WebApplication.CreateBuilder(args);

// Ba�lant� dizesini appsettings.json'dan al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddDbContext<Context>(options =>
    options.UseSqlServer(connectionString));  // DbContext'i yap�land�r

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Controller route ve static asset'leri d�zenleyelim
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
