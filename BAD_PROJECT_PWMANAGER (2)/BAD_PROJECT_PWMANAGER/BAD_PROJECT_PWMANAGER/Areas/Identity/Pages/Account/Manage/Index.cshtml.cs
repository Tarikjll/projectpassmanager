// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BAD_PROJECT_PWMANAGER.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }
        public string CurrentProfileImageSrc { get; set; } = "/images/default-avatar.jpg";

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
            [StringLength(30, MinimumLength = 3, ErrorMessage = "Gebruikersnaam moet tussen 3 en 30 tekens lang zijn.")]
            [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Gebruikersnaam mag enkel letters, cijfers en underscores bevatten.")]
            [Display(Name = "Gebruikersnaam")]
            public string UserName { get; set; } = string.Empty;

            [Display(Name = "E-mailadres")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Voornaam is verplicht.")]
            [Display(Name = "Voornaam")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Achternaam is verplicht.")]
            [Display(Name = "Achternaam")]
            public string LastName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Geef een geldig telefoonnummer in.")]
            [Display(Name = "Telefoonnummer")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Profielfoto")]
            public IFormFile? ProfileImage { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);


            Username = userName ?? string.Empty;

            Input = new InputModel
            {
                UserName = userName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            if (user.ProfileImageData != null &&
                user.ProfileImageData.Length > 0 &&
                !string.IsNullOrWhiteSpace(user.ProfileImageContentType))
            {
                var base64 = Convert.ToBase64String(user.ProfileImageData);
                CurrentProfileImageSrc = $"data:{user.ProfileImageContentType};base64,{base64}";
            }
            else
            {
                CurrentProfileImageSrc = "/images/default-avatar.jpg";
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Gebruiker met ID '{_userManager.GetUserId(User)}' kon niet geladen worden.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return NotFound("Gebruiker niet gevonden.");
                }

                if (!ModelState.IsValid)
                {
                    await LoadAsync(user);
                    return Page();
                }

                if (!string.Equals(user.UserName, Input.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUser = await _userManager.FindByNameAsync(Input.UserName);

                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("Input.UserName", "Deze gebruikersnaam is al in gebruik.");
                        await LoadAsync(user);
                        return Page();
                    }

                    var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.UserName);

                    if (!setUserNameResult.Succeeded)
                    {
                        foreach (var error in setUserNameResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        await LoadAsync(user);
                        return Page();
                    }
                }

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.PhoneNumber = Input.PhoneNumber;

                if (Input.ProfileImage != null && Input.ProfileImage.Length > 0)
                {
                    var allowedContentTypes = new[]
                    {
                        "image/jpeg",
                        "image/png",
                        "image/webp"
                    };

                    if (!allowedContentTypes.Contains(Input.ProfileImage.ContentType))
                    {
                        ModelState.AddModelError("Input.ProfileImage", "Alleen JPG, PNG of WEBP-bestanden zijn toegestaan.");
                        await LoadAsync(user);
                        return Page();
                    }

                    if (Input.ProfileImage.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("Input.ProfileImage", "De profielfoto mag maximaal 2 MB zijn.");
                        await LoadAsync(user);
                        return Page();
                    }

                    using var memoryStream = new MemoryStream();
                    await Input.ProfileImage.CopyToAsync(memoryStream);

                    user.ProfileImageData = memoryStream.ToArray();
                    user.ProfileImageContentType = Input.ProfileImage.ContentType;
                    user.ProfileImagePath = null;
                }

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await LoadAsync(user);
                    return Page();
                }

                await _signInManager.RefreshSignInAsync(user);

                StatusMessage = "Je profiel is bijgewerkt.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Er ging iets mis: {ex.Message}");
                return Page();
            }
        }
    }
}
