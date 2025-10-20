//using Microsoft.EntityFrameworkCore;

//namespace WebDiaryApp.Models
//{
//	public class DiaryContext : DbContext
//	{
//		public DiaryContext(DbContextOptions<DiaryContext> options) : base(options) { }

//		public DbSet<DiaryEntry> DiaryEntries { get; set; }
//	}
//}
using Microsoft.EntityFrameworkCore;

namespace WebDiaryApp.Models
{
	public class DiaryContext : DbContext
	{
		public DiaryContext(DbContextOptions<DiaryContext> options)
			: base(options)
		{
		}

		public DbSet<DiaryEntry> DiaryEntries { get; set; }
	}

	public class DiaryEntry
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
