using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;
using WebDiaryApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Npgsql; // ← Supabase対応に必要

//var builder = WebApplication.CreateBuilder(args);
//Renderファイル監視作りすぎ対策
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	Args = args,
	ContentRootPath = Directory.GetCurrentDirectory()
});

builder.Configuration
	.Sources.Clear();

builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
				 optional: true, reloadOnChange: false)
	.AddEnvironmentVariables();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// --- 接続文字列の設定 ---
// 環境変数 DATABASE_URL（Render/Supabase用）があればそれを使用。
// なければ appsettings.json の値を利用。
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
	?? builder.Configuration.GetConnectionString("DefaultConnection");

// --- SupabaseなどのURLをNpgsql接続文字列に変換 ---
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

// --- HttpClient を使用可能にする（Supabaseアップロード用） ---
builder.Services.AddHttpClient();

var app = builder.Build();

// --- ロケール設定（日本語） ---
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

// --- マイグレーションを自動適用 ---
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.SetCommandTimeout(180);
	db.Database.Migrate();
}

app.Run();
