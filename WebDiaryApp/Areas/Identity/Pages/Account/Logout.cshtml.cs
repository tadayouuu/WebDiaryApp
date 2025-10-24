// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

namespace WebDiaryApp.Areas.Identity.Pages.Account
{
	public class LogoutModel : PageModel
	{
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly ILogger<LogoutModel> _logger;

		public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
		{
			_signInManager = signInManager;
			_logger = logger;
		}

		public async Task<IActionResult> OnPost(string returnUrl = null)
		{
			// サインアウト処理
			await _signInManager.SignOutAsync();

			// Identity認証Cookieを明示的に削除（スマホの再ログイン対策）
			await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
			HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");

			// ログ出力を広島弁で
			_logger.LogInformation("ユーザーがログアウトしたで。セッションもきれいに消したけぇの。");

			if (returnUrl != null)
			{
				return LocalRedirect(returnUrl);
			}
			else
			{
				// 新しいリクエストでユーザー状態を更新するためにリダイレクト
				return RedirectToPage();
			}
		}
	}
}
