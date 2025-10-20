using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;

namespace WebDiaryApp.Data
{
	//public class ApplicationDbContext : DbContext
	//{
	//	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	//		: base(options)
	//	{
	//	}

	//	// DiaryEntry テーブル
	//	public DbSet<DiaryEntry> DiaryEntries { get; set; }
	//}

	public class ApplicationDbContext : IdentityDbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<DiaryEntry> DiaryEntries { get; set; }
	}
}
