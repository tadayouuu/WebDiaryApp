using System;

namespace WebDiaryApp.Models
{
	public class DiaryEntry
	{
		public int Id { get; set; }
		public DateTime? Date { get; set; }   // nullable にしてフォーム空送信を防ぐ
		public string Title { get; set; }
		public string Content { get; set; }
		public string Tag { get; set; }
	}
}
