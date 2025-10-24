using System;
using System.ComponentModel.DataAnnotations;

namespace WebDiaryApp.Models
{
	public class DiaryEntry
	{
		public int Id { get; set; }

		[Required]
		public string Title { get; set; } = string.Empty;

		// 旧 Tag プロパティの代わりに Category として追加
		public string Category { get; set; } = string.Empty;
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
			_createdAt = DateTime.UtcNow;
		}

		// 🧩 追加部分（ユーザーごとの日記を識別）
		public string? UserId { get; set; }
	}
}
