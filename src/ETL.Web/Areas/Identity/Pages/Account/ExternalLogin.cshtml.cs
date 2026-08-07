// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ETL.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ETL.Web.Areas.Identity.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<AppIdentityUser> _signInManager;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<AppIdentityUser> signInManager,
        UserManager<AppIdentityUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ProviderDisplayName { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public IActionResult OnGet() => RedirectToPage("./Login");

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Try to sign in with the existing external login link
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        // No existing account — create one automatically using the Google email
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppIdentityUser { UserName = email, Email = email };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    ProviderDisplayName = info.ProviderDisplayName;
                    ReturnUrl = returnUrl;
                    return Page();
                }
            }

            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        // Fallback: ask the user to supply an email (rare — Google always provides one)
        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        Input = new InputModel { Email = email ?? string.Empty };
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid)
        {
            var user = new AppIdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }
}
