using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using WebDiaryApp.Data;
using WebDiaryApp.Models;

namespace WebDiaryApp.Controllers
{
	[Authorize]
	public class DiaryController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _config;

		public DiaryController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
		{
			_context = context;
			_httpClientFactory = httpClientFactory;
			_config = config;
		}

		// 一覧表示
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			var entries = await _context.DiaryEntries
				.Where(d => d.UserId == userId)
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
				diaryEntry.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				diaryEntry.CreatedAt = DateTime.UtcNow;

				_context.Add(diaryEntry);
				await _context.SaveChangesAsync();
				TempData["FlashMessage"] = "日記を作成しました！";
				return RedirectToAction(nameof(Index));
			}
			return View(diaryEntry);
		}

		// 編集フォーム
		public async Task<IActionResult> Edit(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// 編集処理
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category")] DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
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

		// 削除処理
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

		// プレビュー表示
		public IActionResult Preview(int id)
		{
			var entry = _context.DiaryEntries.Find(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// 📸 Supabase画像アップロード機能
		[HttpPost]
		public async Task<IActionResult> UploadImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("ファイルが選択されていません。");

			var supabaseUrl = _config["SUPABASE_URL"];
			var supabaseKey = _config["SUPABASE_KEY"];
			var bucket = "images";

			var client = _httpClientFactory.CreateClient();
			var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
			var path = $"uploads/{uniqueName}";

			var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{path}";

			using (var content = new StreamContent(file.OpenReadStream()))
			{
				content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

				var response = await client.PostAsync(uploadUrl, content);
				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					return StatusCode((int)response.StatusCode, error);
				}
			}

			var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
			return Ok(new { imageUrl = publicUrl });
		}

		private bool DiaryEntryExists(int id)
		{
			return _context.DiaryEntries.Any(e => e.Id == id);
		}
	}
}
