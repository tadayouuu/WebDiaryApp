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

		// 一覧
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var entries = await _context.DiaryEntries
				.Where(d => d.UserId == userId)
				.OrderByDescending(d => d.CreatedAt)
				.ToListAsync();
			return View(entries);
		}

		// 編集処理（Attach方式）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return NotFound();
			if (!ModelState.IsValid) return RedirectToAction(nameof(Index));

			var existing = await _context.DiaryEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
			if (existing == null) return NotFound();

			var oldImageUrl = existing.ImageUrl;
			var newImageUrl = diaryEntry.ImageUrl;
			var imageChanged = !string.Equals(oldImageUrl, newImageUrl, StringComparison.OrdinalIgnoreCase);

			// 新しいエンティティをAttachして部分更新
			_context.Attach(diaryEntry);
			_context.Entry(diaryEntry).Property(e => e.Title).IsModified = true;
			_context.Entry(diaryEntry).Property(e => e.Content).IsModified = true;
			_context.Entry(diaryEntry).Property(e => e.Category).IsModified = true;
			_context.Entry(diaryEntry).Property(e => e.ImageUrl).IsModified = true;

			await _context.SaveChangesAsync();

			if (imageChanged && !string.IsNullOrEmpty(oldImageUrl))
			{
				await DeleteImageFromSupabaseAsync(oldImageUrl);
			}

			TempData["FlashMessage"] = "日記を更新しました！";
			return RedirectToAction(nameof(Index));
		}

		// 画像削除（Attach方式）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteImage(int id)
		{
			var existing = await _context.DiaryEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
			if (existing == null)
			{
				TempData["FlashMessage"] = "対象の日記が見つかりません。";
				return RedirectToAction("Index");
			}

			if (!string.IsNullOrEmpty(existing.ImageUrl))
			{
				await DeleteImageFromSupabaseAsync(existing.ImageUrl);
				existing.ImageUrl = null;

				// AttachしてImageUrlだけ更新
				_context.Attach(existing);
				_context.Entry(existing).Property(e => e.ImageUrl).IsModified = true;
				await _context.SaveChangesAsync();
			}

			TempData["FlashMessage"] = "画像を削除しました！";
			return RedirectToAction("Index");
		}

		// 新規作成
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
		{
			if (!ModelState.IsValid) return View(diaryEntry);
			diaryEntry.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			diaryEntry.CreatedAt = DateTime.UtcNow;
			_context.Add(diaryEntry);
			await _context.SaveChangesAsync();
			TempData["FlashMessage"] = "日記を作成しました！";
			return RedirectToAction(nameof(Index));
		}

		// 削除
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry != null)
			{
				if (!string.IsNullOrEmpty(entry.ImageUrl))
					await DeleteImageFromSupabaseAsync(entry.ImageUrl);

				_context.DiaryEntries.Remove(entry);
				await _context.SaveChangesAsync();
				TempData["FlashMessage"] = "日記を削除しました！";
			}
			return RedirectToAction(nameof(Index));
		}

		// Supabaseアップロード
		[HttpPost]
		public async Task<IActionResult> UploadImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("ファイルが選択されていません。");

			var supabaseUrl = _config["SUPABASE_URL"];
			var supabaseKey = _config["SUPABASE_SERVICE_ROLE"] ?? _config["SUPABASE_KEY"];
			var bucket = "images";
			var client = _httpClientFactory.CreateClient();

			var safeFileName = Uri.EscapeDataString($"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}");
			var path = $"uploads/{safeFileName}";
			var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{path}";

			using var content = new StreamContent(file.OpenReadStream());
			content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
			content.Headers.Add("x-upsert", "false");

			var response = await client.PostAsync(uploadUrl, content);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync();
				return StatusCode((int)response.StatusCode, $"Storageエラー {response.StatusCode}: {body}");
			}

			var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
			return Ok(new { imageUrl = publicUrl });
		}

		// Supabase画像削除
		private async Task DeleteImageFromSupabaseAsync(string imageUrl)
		{
			try
			{
				var supabaseUrl = _config["SUPABASE_URL"] ?? "https://klkhzamffrmkvyeiubeo.supabase.co";
				var supabaseKey = _config["SUPABASE_SERVICE_ROLE"]
					?? _config["SUPABASE_SERVICE_KEY"]
					?? _config["SUPABASE_KEY"];

				var client = new Supabase.Client(supabaseUrl, supabaseKey);
				await client.InitializeAsync();

				var uri = new Uri(imageUrl);
				var path = uri.AbsolutePath.Replace("/storage/v1/object/public/images/", "");
				path = Uri.UnescapeDataString(path);
				var storage = client.Storage.From("images");
				await storage.Remove(new List<string> { path });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Warn] Supabase画像削除失敗: {ex.Message}");
			}
		}
	}
}
