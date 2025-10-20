//using System;
//using System.ComponentModel.DataAnnotations;

//namespace WebDiaryApp.Models
//{
//	public class DiaryEntry
//	{
//		public int Id { get; set; }

//		[Display(Name = "日付")]
//		public DateTime? Date { get; set; } = DateTime.Now; // 作成時のみセット
//		public string Title { get; set; }
//		public string Content { get; set; }
//		public string Tag { get; set; }
//	}
//}

using System;
using System.ComponentModel.DataAnnotations;

namespace WebDiaryApp.Models
{
	public class DiaryEntry
	{
		public int Id { get; set; }

		[Required]
		public string Title { get; set; } = string.Empty;

		public string Content { get; set; } = string.Empty;

		private DateTime _createdAt;

		[Required]
		public DateTime CreatedAt
		{
			get => _createdAt;
			set => _createdAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
		}

		public DiaryEntry()
		{
			// 新規作成時は自動で UTC 時刻に
			_createdAt = DateTime.UtcNow;
		}
	}
}
