using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using WebDiaryApp.Data;

// Renderファイル監視作りすぎ対策
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	Args = args,
	ContentRootPath = Directory.GetCurrentDirectory()
});

// RenderのPORTで待ち受け
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
	builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// 設定ファイル（reloadなし）
builder.Configuration.Sources.Clear();
builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
	.AddEnvironmentVariables();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// 接続文字列（RenderのDATABASE_URL優先）
var connectionString =
	Environment.GetEnvironmentVariable("DATABASE_URL")
	?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
	throw new InvalidOperationException("Connection string is missing. Set DATABASE_URL or DefaultConnection.");

// URI形式なら Npgsql 形式へ変換
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
	connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
	var uri = new Uri(connectionString);
	var userInfo = uri.UserInfo.Split(':', 2);

	connectionString = new NpgsqlConnectionStringBuilder
	{
		Host = uri.Host,
		Port = uri.Port,
		Username = userInfo[0],
		Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
		Database = uri.AbsolutePath.TrimStart('/'),
		SslMode = SslMode.Require,
		TrustServerCertificate = true,
	}.ConnectionString;
}

// DbContext（ログイン時に詰まらんようにタイムアウト＆リトライ）
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString, npgsqlOptions =>
	{
		npgsqlOptions.CommandTimeout(180);
		npgsqlOptions.EnableRetryOnFailure();
	}));

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
	options.SignIn.RequireConfirmedEmail = false; // 念押し
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Cookie（RenderのHTTPS終端対策）
builder.Services.ConfigureApplicationCookie(options =>
{
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// DataProtection Keys 永続化（Renderで /var/data をPersistent Diskにしてる前提）
builder.Services.AddDataProtection()
	.PersistKeysToFileSystem(new DirectoryInfo("/var/data/dpkeys"))
	.SetApplicationName("WebDiaryApp");

// HttpClient（既存用途）
builder.Services.AddHttpClient();

var app = builder.Build();

// Renderのリバースプロキシ対応（UseHttpsRedirectionより前）
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ロケール設定（日本語）
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

// ルーティング
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Diary}/{action=Index}/{id?}");
app.MapRazorPages();

// Migration（本番はENVで明示的に true にした時だけ）
var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true";
if (runMigrations)
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.SetCommandTimeout(180);
	db.Database.Migrate();
}

app.Run();
