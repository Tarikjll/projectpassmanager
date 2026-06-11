// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BAD_PROJECT_PWMANAGER.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
            [StringLength(30, MinimumLength = 3, ErrorMessage = "Gebruikersnaam moet tussen 3 en 30 tekens lang zijn.")]
            [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Gebruikersnaam mag enkel letters, cijfers en underscores bevatten.")]
            [Display(Name = "Gebruikersnaam")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Voornaam is verplicht.")]
            [Display(Name = "Voornaam")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Achternaam is verplicht.")]
            [Display(Name = "Achternaam")]
            public string LastName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Geef een geldig telefoonnummer in.")]
            [Display(Name = "Telefoonnummer")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-mailadres is verplicht.")]
            [EmailAddress(ErrorMessage = "Geef een geldig e-mailadres in.")]
            [Display(Name = "E-mailadres")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Wachtwoord is verplicht.")]
            [StringLength(100, ErrorMessage = "Het wachtwoord moet minstens {2} en maximaal {1} tekens lang zijn.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bevestig wachtwoord")]
            [Compare("Password", ErrorMessage = "De wachtwoorden komen niet overeen.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var existingUserName = await _userManager.FindByNameAsync(Input.UserName);

                if (existingUserName != null)
                {
                    ModelState.AddModelError("Input.UserName", "Deze gebruikersnaam is al in gebruik.");
                    return Page();
                }

                var existingEmail = await _userManager.FindByEmailAsync(Input.Email);

                if (existingEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Dit e-mailadres is al in gebruik.");
                    return Page();
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.PhoneNumber = Input.PhoneNumber;
                user.EmailConfirmed = true;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "FreeUser");

                    _logger.LogInformation("Gebruiker maakte een nieuw account met wachtwoord.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code, returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Bevestig je e-mailadres",
                        $"Bevestig je account door <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>hier te klikken</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Kan geen instantie van '{nameof(ApplicationUser)}' maken. " +
                    $"Controleer dat '{nameof(ApplicationUser)}' geen abstracte klasse is en een parameterloze constructor heeft.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("De standaard-UI vereist een user store met e-mailondersteuning.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
