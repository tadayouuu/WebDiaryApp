using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Data;
using WebDiaryApp.Models;

namespace WebDiaryApp.Controllers
{
	public class DiaryController : Controller
	{
		private readonly ApplicationDbContext _context;

		public DiaryController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 一覧表示
		public async Task<IActionResult> Index()
		{
			var entries = await _context.DiaryEntries
				.OrderByDescending(d => d.CreatedAt)
				.ToListAsync();
			return View(entries);
		}

		// 新規作成フォーム
		public IActionResult Create()
		{
			return View();
		}

		// 新規作成処理
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Title,Content,Category")] DiaryEntry diaryEntry)
		{
			if (ModelState.IsValid)
			{
				// 日付を自動セット（UTC）
				diaryEntry.CreatedAt = DateTime.UtcNow;

				_context.Add(diaryEntry);
				await _context.SaveChangesAsync();
				TempData["FlashMessage"] = "日記を作成しました！";
				return RedirectToAction(nameof(Index));
			}
			return View(diaryEntry);
		}

		// 編集フォーム（カードクリック時）
		public async Task<IActionResult> Edit(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// 編集処理（カード内フォームから）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category")] DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					// 日付は更新しない
					var existing = await _context.DiaryEntries.FindAsync(id);
					if (existing == null) return NotFound();

					existing.Title = diaryEntry.Title;
					existing.Content = diaryEntry.Content;
					existing.Category = diaryEntry.Category;

					_context.Update(existing);
					await _context.SaveChangesAsync();

					TempData["FlashMessage"] = "日記を更新しました！";
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!DiaryEntryExists(diaryEntry.Id)) return NotFound();
					else throw;
				}
				return RedirectToAction(nameof(Index));
			}
			return View(diaryEntry);
		}

		// 削除フォーム（カード内）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry != null)
			{
				_context.DiaryEntries.Remove(entry);
				await _context.SaveChangesAsync();

				TempData["FlashMessage"] = "日記を削除しました！";
			}
			return RedirectToAction(nameof(Index));
		}

		private bool DiaryEntryExists(int id)
		{
			return _context.DiaryEntries.Any(e => e.Id == id);
		}
	}
}
