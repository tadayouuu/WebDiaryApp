using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;

namespace WebDiaryApp.Data
{
	public class ApplicationDbContext : IdentityDbContext, IDataProtectionKeyContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		// ★日記テーブル（これが無いとDiaryControllerが死ぬ）
		public DbSet<DiaryEntry> DiaryEntries { get; set; } = default!;

		// ★DataProtectionキー保存用
		public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
	}
}
