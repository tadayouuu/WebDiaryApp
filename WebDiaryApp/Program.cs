//using Microsoft.EntityFrameworkCore;
//using WebDiaryApp.Models;

//var builder = WebApplication.CreateBuilder(args);

//// MVC + Razor Pages
//builder.Services.AddControllersWithViews();

//// DbContext 登録（必ず Build() の前）
////builder.Services.AddDbContext<DiaryContext>(options =>
////	options.UseSqlite("Data Source=diary.db"));

//builder.Services.AddDbContext<DiaryContext>(options =>
//	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//	app.UseExceptionHandler("/Home/Error");
//	app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthorization();

//// デフォルトルートを Diary/Index に
//app.MapControllerRoute(
//	name: "default",
//	pattern: "{controller=Diary}/{action=Index}/{id?}");

//app.Run();

using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;
using WebDiaryApp.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// DbContext を PostgreSQL 用に登録
//builder.Services.AddDbContext<DiaryContext>(options =>
//	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// デフォルトルート
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Diary}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
}

app.Run();

