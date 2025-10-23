//using Microsoft.EntityFrameworkCore;
//using WebDiaryApp.Models;
//using WebDiaryApp.Data;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Localization;
//using System.Globalization;

//var builder = WebApplication.CreateBuilder(args);

//// MVC + Razor Pages
//builder.Services.AddControllersWithViews();

//// PostgreSQL 接続文字列
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//// DbContext 設定
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//	options.UseNpgsql(connectionString));

//// Identity 設定
//builder.Services.AddDefaultIdentity<IdentityUser>(options =>
//{
//	options.SignIn.RequireConfirmedAccount = false;
//})
//.AddEntityFrameworkStores<ApplicationDbContext>();

//builder.Services.AddControllersWithViews();

//var app = builder.Build();

//// サポートするカルチャを設定
//var supportedCultures = new[] { new CultureInfo("ja-JP") };
//app.UseRequestLocalization(new RequestLocalizationOptions
//{
//	DefaultRequestCulture = new RequestCulture("ja-JP"),
//	SupportedCultures = supportedCultures,
//	SupportedUICultures = supportedCultures
//});

//if (!app.Environment.IsDevelopment())
//{
//	app.UseExceptionHandler("/Home/Error");
//	app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

//// デフォルトルート
//app.MapControllerRoute(
//	name: "default",
//	pattern: "{controller=Diary}/{action=Index}/{id?}");
//app.MapRazorPages(); // Identity のログインページなどを有効化

//using (var scope = app.Services.CreateScope())
//{
//	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//	db.Database.Migrate();
//}

//app.Run();

using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;
using WebDiaryApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Npgsql; // ← Supabase対応で必要

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// --- 接続文字列設定 ---
// 環境変数 DATABASE_URL（Render/Supabase用）を優先し、なければ appsettings.json の値を使用
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// --- SupabaseなどのURL形式をNpgsql形式に変換 ---
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port,
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : "",
        Database = uri.LocalPath.TrimStart('/'),
        SslMode = SslMode.Require,
        TrustServerCertificate = true,
        Pooling = true
    };

    connectionString = npgsqlBuilder.ConnectionString;
}

// --- DbContext 設定 ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- Identity 設定 ---
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// --- ロケール設定 ---
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

// --- ルーティング設定 ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Diary}/{action=Index}/{id?}");
app.MapRazorPages();

// --- マイグレーション自動適用 ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();