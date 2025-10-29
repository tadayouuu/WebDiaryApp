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
		private readonly ILogger<DiaryController> _logger;  // ←★追加

		public DiaryController(
			ApplicationDbContext context,
			IHttpClientFactory httpClientFactory,
			IConfiguration config,
			ILogger<DiaryController> logger) // ←★引数追加
		{
			_context = context;
			_httpClientFactory = httpClientFactory;
			_config = config;
			_logger = logger; // ←★代入追加
		}

		// 一覧
		public async Task<IActionResult> Index(string? category)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var query = _context.DiaryEntries.Where(d => d.UserId == userId);
			if (!string.IsNullOrEmpty(category))
				query = query.Where(d => d.Category == category);
			var entries = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
			ViewBag.SelectedCategory = category;
			return View(entries);
		}

		// 編集フォーム表示
		public async Task<IActionResult> Edit(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// ---- 編集処理 ----
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return NotFound();

			var existing = await _context.DiaryEntries.AsTracking().FirstOrDefaultAsync(e => e.Id == id);
			if (existing == null) return NotFound();

			var oldImageUrl = existing.ImageUrl;
			var newImageUrl = diaryEntry.ImageUrl;

			existing.Title = diaryEntry.Title;
			existing.Content = diaryEntry.Content;
			existing.Category = diaryEntry.Category;
			existing.ImageUrl = newImageUrl;

			await _context.SaveChangesAsync();

			// 🔹 Supabase 側の古い画像削除
			if (!string.IsNullOrEmpty(oldImageUrl) &&
				!string.IsNullOrEmpty(newImageUrl) &&
				!oldImageUrl.Equals(newImageUrl, StringComparison.OrdinalIgnoreCase))
			{
				await DeleteImageFromSupabaseAsync(oldImageUrl);
			}

			TempData["FlashMessage"] = "日記を更新しました！";
			return RedirectToAction("Edit", new { id }); // ← Indexに戻さない
		}

		// 新規作成フォーム
		public IActionResult Create() => View();

		// 新規作成処理
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

		// 日記削除
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

		// ---- 画像単体削除 ----
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteImage(int id)
		{
			var entry = await _context.DiaryEntries.AsTracking().FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null) return NotFound();

			var oldUrl = entry.ImageUrl; // ここで確実にコピー
			if (!string.IsNullOrEmpty(oldUrl))
			{
				// ⚠️ Save前に削除開始
				_logger.LogInformation("[Diary] 単体画像削除開始: {Path}", oldUrl);

				await DeleteImageFromSupabaseAsync(oldUrl);  // ← 先に削除してもDBは無関係なので安全

				entry.ImageUrl = null;
				await _context.SaveChangesAsync();

				_logger.LogInformation("[Diary] DB上のImageUrlをnullに更新しました (ID={Id})", id);
			}
			else
			{
				_logger.LogWarning("[Diary] DeleteImage呼び出し時にImageUrlが空でした (ID={Id})", id);
			}

			TempData["FlashMessage"] = "画像を削除しました！";
			return RedirectToAction("Edit", new { id });
		}

		// Supabase アップロード
		[HttpPost]
		public async Task<IActionResult> UploadImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("ファイルが選択されていません。");

			var supabaseUrl = _config["SUPABASE_URL"];
			var supabaseKey = _config["SUPABASE_SERVICE_ROLE"] ?? _config["SUPABASE_KEY"];
			var bucket = "images";

			if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
				return StatusCode(500, "Supabase設定が未設定です。");

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

		// Supabase 画像削除共通
		private async Task DeleteImageFromSupabaseAsync(string imageUrl)
		{
			try
			{
				var supabaseUrl = _config["SUPABASE_URL"] ?? "https://klkhzamffrmkvyeiubeo.supabase.co";
				var supabaseKey = _config["SUPABASE_SERVICE_ROLE"]
					?? _config["SUPABASE_SERVICE_KEY"]
					?? _config["SUPABASE_KEY"];

				if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
				{
					_logger.LogWarning("Supabaseの環境変数が不足しています。削除スキップ。");
					return;
				}

				var options = new Supabase.SupabaseOptions
				{
					AutoConnectRealtime = false,
					AutoRefreshToken = false
				};

				var client = new Supabase.Client(supabaseUrl, supabaseKey, options);
				await client.InitializeAsync();

				var uri = new Uri(imageUrl);
				var prefix = "/storage/v1/object/public/images/";
				var path = uri.AbsolutePath.StartsWith(prefix)
					? uri.AbsolutePath.Substring(prefix.Length)
					: uri.AbsolutePath;
				path = Uri.UnescapeDataString(path);

				_logger.LogInformation("[Supabase] 削除対象パス: {Path}", path);

				var storage = client.Storage.From("images");
				var result = await storage.Remove(new List<string> { path });

				if (result != null && result.Count > 0)
				{
					foreach (var item in result)
					{
						_logger.LogInformation("[Supabase] 削除成功: {Name}", item?.Name ?? "(null)");
					}
				}
				else
				{
					_logger.LogWarning("[Supabase] 削除結果が空でした。パス={Path}", path);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Supabase] 画像削除中に例外発生");
			}
		}
	}
}
