//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Net.Http.Headers;
//using WebDiaryApp.Data;
//using WebDiaryApp.Models;

//namespace WebDiaryApp.Controllers
//{
//	[Authorize]
//	public class DiaryController : Controller
//	{
//		private readonly ApplicationDbContext _context;
//		private readonly IHttpClientFactory _httpClientFactory;
//		private readonly IConfiguration _config;

//		public DiaryController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
//		{
//			_context = context;
//			_httpClientFactory = httpClientFactory;
//			_config = config;
//		}

//		// 一覧表示
//		public async Task<IActionResult> Index()
//		{
//			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

//			var entries = await _context.DiaryEntries
//				.Where(d => d.UserId == userId)
//				.OrderByDescending(d => d.CreatedAt)
//				.ToListAsync();

//			return View(entries);
//		}

//		// 新規作成フォーム
//		public IActionResult Create()
//		{
//			return View();
//		}

//		// 新規作成処理
//		[HttpPost]
//		[ValidateAntiForgeryToken]
//		public async Task<IActionResult> Create([Bind("Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
//		{
//			if (ModelState.IsValid)
//			{
//				diaryEntry.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
//				diaryEntry.CreatedAt = DateTime.UtcNow;

//				_context.Add(diaryEntry);
//				await _context.SaveChangesAsync();
//				TempData["FlashMessage"] = "日記を作成しました！";
//				return RedirectToAction(nameof(Index));
//			}
//			return View(diaryEntry);
//		}

//		// 編集フォーム
//		public async Task<IActionResult> Edit(int id)
//		{
//			var entry = await _context.DiaryEntries.FindAsync(id);
//			if (entry == null) return NotFound();
//			return View(entry);
//		}

//		// 編集処理
//		// 編集処理
//		[HttpPost]
//		[ValidateAntiForgeryToken]
//		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
//		{
//			if (id != diaryEntry.Id) return NotFound();

//			if (ModelState.IsValid)
//			{
//				var existing = await _context.DiaryEntries.FindAsync(id);
//				if (existing == null) return NotFound();

//				var oldImageUrl = existing.ImageUrl;
//				var newImageUrl = diaryEntry.ImageUrl;

//				existing.Title = diaryEntry.Title;
//				existing.Content = diaryEntry.Content;
//				existing.Category = diaryEntry.Category;
//				existing.ImageUrl = newImageUrl;

//				await _context.SaveChangesAsync();

//				// 🔹 古い画像の削除をファイル名比較で判定
//				if (!string.IsNullOrEmpty(oldImageUrl) && !string.IsNullOrEmpty(newImageUrl))
//				{
//					var oldFile = Path.GetFileName(oldImageUrl);
//					var newFile = Path.GetFileName(newImageUrl);

//					if (!oldFile.Equals(newFile, StringComparison.OrdinalIgnoreCase))
//					{
//						await DeleteImageFromSupabaseAsync(oldImageUrl);
//					}
//				}

//				TempData["FlashMessage"] = "日記を更新しました！";
//				return RedirectToAction(nameof(Index));
//			}

//			return View(diaryEntry);
//		}

//		// 削除処理
//		[HttpPost]
//		[ValidateAntiForgeryToken]
//		public async Task<IActionResult> Delete(int id)
//		{
//			var entry = await _context.DiaryEntries.FindAsync(id);
//			if (entry != null)
//			{
//				if (!string.IsNullOrEmpty(entry.ImageUrl))
//				{
//					await DeleteImageFromSupabaseAsync(entry.ImageUrl);
//				}

//				_context.DiaryEntries.Remove(entry);
//				await _context.SaveChangesAsync();

//				TempData["FlashMessage"] = "日記を削除しました！";
//			}
//			return RedirectToAction(nameof(Index));
//		}

//		// プレビュー表示
//		public IActionResult Preview(int id)
//		{
//			var entry = _context.DiaryEntries.Find(id);
//			if (entry == null) return NotFound();
//			return View(entry);
//		}

//		// 📸 Supabase画像アップロード機能
//		[HttpPost]
//		public async Task<IActionResult> UploadImage(IFormFile file)
//		{
//			if (file == null || file.Length == 0)
//				return BadRequest("ファイルが選択されていません。");

//			var supabaseUrl = _config["SUPABASE_URL"];
//			// service_role があれば優先、なければ従来の anon を使う
//			var supabaseKey = _config["SUPABASE_SERVICE_ROLE"] ?? _config["SUPABASE_KEY"];
//			var bucket = "images";

//			if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
//				return StatusCode(500, "Supabaseの環境変数(SUPABASE_URL / SUPABASE_SERVICE_ROLE もしくは SUPABASE_KEY)が未設定です。");

//			var client = _httpClientFactory.CreateClient();

//			// ファイル名にスペースや日本語があると安全のためURLエンコード
//			var safeFileName = Uri.EscapeDataString($"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}");
//			var path = $"uploads/{safeFileName}";

//			var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{path}";

//			using var content = new StreamContent(file.OpenReadStream());
//			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

//			// 認可ヘッダ（service_role だと確実に通る）
//			client.DefaultRequestHeaders.Authorization =
//				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);

//			// 上書きしたくない場合は x-upsert: false（既定falseだが明示）
//			content.Headers.Add("x-upsert", "false");

//			HttpResponseMessage response;
//			try
//			{
//				response = await client.PostAsync(uploadUrl, content);
//			}
//			catch (Exception ex)
//			{
//				return StatusCode(500, $"アップロード通信で例外: {ex.Message}");
//			}

//			if (!response.IsSuccessStatusCode)
//			{
//				var body = await response.Content.ReadAsStringAsync();
//				// 典型: 401/403 は権限不足（anonキーでInsert不可など）
//				return StatusCode((int)response.StatusCode, $"Storageエラー {response.StatusCode}: {body}");
//			}

//			var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
//			return Ok(new { imageUrl = publicUrl });
//		}

//		//共通削除メソッド
//		private async Task DeleteImageFromSupabaseAsync(string imageUrl)
//		{
//			try
//			{
//				var supabaseUrl = _config["SUPABASE_URL"] ?? "https://klkhzamffrmkvyeiubeo.supabase.co";
//				var supabaseKey = _config["SUPABASE_SERVICE_ROLE"]
//					?? _config["SUPABASE_SERVICE_KEY"]
//					?? _config["SUPABASE_KEY"];

//				var client = new Supabase.Client(supabaseUrl, supabaseKey);
//				await client.InitializeAsync();

//				var uri = new Uri(imageUrl);
//				var path = uri.AbsolutePath.Replace("/storage/v1/object/public/images/", "");
//				path = Uri.UnescapeDataString(path);

//				Console.WriteLine($"[Supabase] 削除対象パス: {path}");
//				var storage = client.Storage.From("images");
//				var result = await storage.Remove(new List<string> { path });

//				Console.WriteLine($"[Supabase] 削除完了: {result?.Count ?? 0} 件");
//			}
//			catch (Exception ex)
//			{
//				Console.WriteLine($"[Warn] Supabase画像削除に失敗: {ex.Message}");
//			}
//		}

//		//画像単体削除
//		[HttpPost]
//		[ValidateAntiForgeryToken]
//		public async Task<IActionResult> DeleteImage(int id)
//		{
//			// DBから追跡付きで取得
//			var entry = await _context.DiaryEntries.FirstOrDefaultAsync(e => e.Id == id);
//			if (entry == null)
//			{
//				TempData["FlashMessage"] = "対象の日記が見つかりません。";
//				return RedirectToAction("Index");
//			}

//			if (!string.IsNullOrEmpty(entry.ImageUrl))
//			{
//				await DeleteImageFromSupabaseAsync(entry.ImageUrl);
//				entry.ImageUrl = null;

//				// ✅ 他のカラムは触らずに ImageUrl だけ更新
//				_context.Entry(entry).Property(e => e.ImageUrl).IsModified = true;
//				await _context.SaveChangesAsync();
//			}

//			TempData["FlashMessage"] = "画像を削除しました！";
//			return RedirectToAction("Index");
//		}

//		private bool DiaryEntryExists(int id)
//		{
//			return _context.DiaryEntries.Any(e => e.Id == id);
//		}
//	}
//}

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

		// 編集フォーム（Index内で使う）
		public async Task<IActionResult> Edit(int id)
		{
			var entry = await _context.DiaryEntries.FindAsync(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// 編集処理
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category,ImageUrl")] DiaryEntry diaryEntry)
		{
			if (id != diaryEntry.Id) return NotFound();
			if (!ModelState.IsValid) return View(diaryEntry);

			var existing = await _context.DiaryEntries.FindAsync(id);
			if (existing == null) return NotFound();

			var oldImageUrl = existing.ImageUrl;
			var newImageUrl = diaryEntry.ImageUrl;

			existing.Title = diaryEntry.Title;
			existing.Content = diaryEntry.Content;
			existing.Category = diaryEntry.Category;
			existing.ImageUrl = newImageUrl;

			await _context.SaveChangesAsync();

			if (!string.IsNullOrEmpty(oldImageUrl) && !string.IsNullOrEmpty(newImageUrl))
			{
				var oldFile = Path.GetFileName(oldImageUrl);
				var newFile = Path.GetFileName(newImageUrl);
				if (!oldFile.Equals(newFile, StringComparison.OrdinalIgnoreCase))
					await DeleteImageFromSupabaseAsync(oldImageUrl);
			}

			TempData["FlashMessage"] = "日記を更新しました！";
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

		// プレビュー表示
		public IActionResult Preview(int id)
		{
			var entry = _context.DiaryEntries.Find(id);
			if (entry == null) return NotFound();
			return View(entry);
		}

		// Supabase画像アップロード
		[HttpPost]
		public async Task<IActionResult> UploadImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("ファイルが選択されていません。");

			var supabaseUrl = _config["SUPABASE_URL"];
			var supabaseKey = _config["SUPABASE_SERVICE_ROLE"] ?? _config["SUPABASE_KEY"];
			var bucket = "images";

			if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
				return StatusCode(500, "Supabase設定が不足しています。");

			var client = _httpClientFactory.CreateClient();
			var safeFileName = Uri.EscapeDataString($"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}");
			var path = $"uploads/{safeFileName}";
			var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{path}";

			using var content = new StreamContent(file.OpenReadStream());
			content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
			content.Headers.Add("x-upsert", "false");

			HttpResponseMessage response;
			try
			{
				response = await client.PostAsync(uploadUrl, content);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"通信エラー: {ex.Message}");
			}

			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync();
				return StatusCode((int)response.StatusCode, $"Storageエラー {response.StatusCode}: {body}");
			}

			var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
			return Ok(new { imageUrl = publicUrl });
		}

		// Supabase画像削除共通処理
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
				Console.WriteLine($"[Warn] Supabase画像削除に失敗: {ex.Message}");
			}
		}

		// 画像単体削除
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteImage(int id)
		{
			var entry = await _context.DiaryEntries.FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null)
			{
				TempData["FlashMessage"] = "対象の日記が見つかりません。";
				return RedirectToAction("Index");
			}

			if (!string.IsNullOrEmpty(entry.ImageUrl))
			{
				await DeleteImageFromSupabaseAsync(entry.ImageUrl);
				entry.ImageUrl = null;

				// ImageUrlのみを部分更新
				_context.Entry(entry).Property(e => e.ImageUrl).IsModified = true;
				await _context.SaveChangesAsync();
			}

			TempData["FlashMessage"] = "画像を削除しました！";
			return RedirectToAction("Index");
		}

		private bool DiaryEntryExists(int id) =>
			_context.DiaryEntries.Any(e => e.Id == id);
	}
}
