using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BAD_PROJECT_PWMANAGER.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginModel> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Gebruikersnaam of e-mailadres is verplicht.")]
        [Display(Name = "Gebruikersnaam of e-mailadres")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht.")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Onthoud mij")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ApplicationUser? user;

        if (Input.Login.Contains("@"))
        {
            user = await _userManager.FindByEmailAsync(Input.Login);
        }
        else
        {
            user = await _userManager.FindByNameAsync(Input.Login);
        }

        if (user == null)
        {
            LogAttempt(Input.Login, false, "Ongeldige loginpoging.");
            ModelState.AddModelError(string.Empty, "Ongeldige loginpoging.");
            return Page();
        }

        if (user.IsBanned)
        {
            LogAttempt(Input.Login, false, UiText.T("Account.Banned"), user.Id);
            ModelState.AddModelError(string.Empty, UiText.T("Account.Banned"));
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            LogAttempt(Input.Login, true, null, user.Id);
            _logger.LogInformation("Gebruiker aangemeld.");
            return LocalRedirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            LogAttempt(Input.Login, false, "Account vergrendeld door te veel mislukte pogingen.", user.Id);
            _logger.LogWarning("Gebruikersaccount vergrendeld.");
            return RedirectToPage("./Lockout");
        }

        LogAttempt(Input.Login, false, "Ongeldig wachtwoord.", user.Id);
        ModelState.AddModelError(string.Empty, "Ongeldige loginpoging.");
        return Page();
    }

    private void LogAttempt(string loginIdentifier, bool succeeded, string? failureReason, string? userId = null)
    {
        _dbContext.LoginAttempts.Add(new LoginAttempt
        {
            LoginIdentifier = loginIdentifier,
            UserId = userId,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            AttemptedAtUtc = DateTime.UtcNow
        });

        _dbContext.SaveChanges();
    }
}
