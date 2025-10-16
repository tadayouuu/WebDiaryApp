using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDiaryApp.Models;

namespace WebDiaryApp.Controllers
{
	public class DiaryController : Controller
	{
		private readonly DiaryContext _context;

		public DiaryController(DiaryContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var entries = await _context.DiaryEntries
				.OrderByDescending(d => d.Date)
				.ToListAsync();
			return View(entries);
		}

		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(DiaryEntry diaryEntry)
		{
			if (ModelState.IsValid)
			{
				_context.Add(diaryEntry);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(diaryEntry);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return BadRequest();
			if (ModelState.IsValid)
			{
				_context.Update(diaryEntry);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(diaryEntry);
		}

		public async Task<IActionResult> Delete(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if(entry == null) return NotFound();
			return View(entry);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if(entry != null)
			{
				_context.DiaryEntries.Remove(entry);
				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}
	}
}
