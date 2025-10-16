using Microsoft.EntityFrameworkCore;

namespace WebDiaryApp.Models
{
	public class DiaryContext : DbContext
	{
		public DiaryContext(DbContextOptions<DiaryContext> options) : base(options) { }

		public DbSet<DiaryEntry> DiaryEntries { get; set; }
	}
}
