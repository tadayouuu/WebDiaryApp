using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();

// DbContext 登録（必ず Build() の前）
builder.Services.AddDbContext<DiaryContext>(options =>
	options.UseSqlite("Data Source=diary.db"));

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

// デフォルトルートを Diary/Index に
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Diary}/{action=Index}/{id?}");

app.Run();
