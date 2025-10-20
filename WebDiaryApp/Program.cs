using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;
using WebDiaryApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// DbContext を PostgreSQL 用に登録
//builder.Services.AddDbContext<DiaryContext>(options =>
//	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// PostgreSQL 接続文字列
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext 設定
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString));

// Identity 設定
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// サポートするカルチャを設定
var supportedCultures = new[] { new CultureInfo("ja-JP") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
	DefaultRequestCulture = new RequestCulture("ja-JP"),
	SupportedCultures = supportedCultures,
	SupportedUICultures = supportedCultures
});

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// デフォルトルート
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Diary}/{action=Index}/{id?}");
app.MapRazorPages(); // Identity のログインページなどを有効化

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
}

app.Run();

