using System;
using System.ComponentModel.DataAnnotations;

namespace WebDiaryApp.Models
{
	public class DiaryEntry
	{
		public int Id { get; set; }

		[Display(Name = "日付")]
		public DateTime? Date { get; set; } = DateTime.Now; // 作成時のみセット
		public string Title { get; set; }
		public string Content { get; set; }
		public string Tag { get; set; }
	}
}
