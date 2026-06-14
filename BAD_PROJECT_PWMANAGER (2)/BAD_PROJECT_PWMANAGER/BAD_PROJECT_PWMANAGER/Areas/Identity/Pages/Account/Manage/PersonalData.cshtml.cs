using System.Text;
using System.Text.Json;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BAD_PROJECT_PWMANAGER.Areas.Identity.Pages.Account.Manage;

public class PersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public PersonalDataModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadPersonalDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var personalData = new Dictionary<string, string?>
        {
            ["UserName"] = await _userManager.GetUserNameAsync(user),
            ["Email"] = user.Email,
            ["FirstName"] = user.FirstName,
            ["LastName"] = user.LastName,
            ["PhoneNumber"] = user.PhoneNumber,
            ["ProfileImagePath"] = user.ProfileImagePath,
            ["ProfileImageContentType"] = user.ProfileImageContentType,
            ["HasProfileImage"] = user.ProfileImageData is { Length: > 0 } ? "true" : "false"
        };

        var json = JsonSerializer.Serialize(personalData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json", "PersonalData.json");
    }

    public async Task<IActionResult> OnPostDeletePersonalDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        await _signInManager.SignOutAsync();
        return Redirect("~/");
    }
}
